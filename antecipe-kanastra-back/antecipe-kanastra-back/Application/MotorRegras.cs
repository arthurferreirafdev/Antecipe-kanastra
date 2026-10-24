using antecipe_kanastra.Domain;
using antecipe_kanastra.Infrastructure;

namespace antecipe_kanastra.Application;

public sealed class MotorRegras(ConfiguracaoService configuracao, EstoqueService estoque,
    PrecificacaoService precificacao, DataBaseOperacao dataBase)
{
    public async Task<ResultadoLote> Processar(IReadOnlyList<Recebivel> lote, bool confirmar, CancellationToken ct)
    {
        var config = configuracao.Carregar();
        var regras = config.Validadores.Where(r => r.Ativo)
            .Select(r => (Config: r, Validador: configuracao.Resolver(r.Codigo))).ToArray();
        await estoque.Exclusao.WaitAsync(ct);
        try
        {
            var carteira = estoque.Snapshot(config.Fundo.Cnpj);
            var contextoBase = new ContextoValidacao(dataBase.Valor, carteira);
            var motivos = Enumerable.Range(0, lote.Count).Select(_ => new List<Motivo>()).ToArray();
            var recusas = new bool[lote.Count];
            var pendencias = new bool[lote.Count];
            var duplicadas = lote.GroupBy(Documentos.Chave, StringComparer.Ordinal)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);

            for (var i = 0; i < lote.Count; i++)
            {
                var r = lote[i];
                if (Documentos.Digitos(r.DocFundo) != Documentos.Digitos(config.Fundo.Cnpj))
                {
                    motivos[i].Add(new Motivo("FUNDO_INCORRETO", "Recebível não pertence ao fundo configurado."));
                    recusas[i] = true;
                }
                if (carteira.Chaves.Contains(Documentos.Chave(r)) || duplicadas.Contains(Documentos.Chave(r)))
                {
                    motivos[i].Add(new Motivo("RECEBIVEL_DUPLICADO", "Recebível já existe no estoque ou está repetido no lote."));
                    recusas[i] = true;
                }
                foreach (var (regra, validador) in regras.Where(x => !x.Validador.EhConcentracao))
                    await Executar(i, r, regra, validador, contextoBase, motivos, recusas, pendencias, ct);
            }

            var selecionadoTotal = 0m;
            var sacados = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var cedentes = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var liquidos = new decimal?[lote.Count];
            var selecionados = new List<Recebivel>();

            // A ordem de seleção é estável e não depende da ordem das linhas do CSV.
            foreach (var i in Enumerable.Range(0, lote.Count).OrderBy(i => Documentos.Chave(lote[i]), StringComparer.Ordinal)
                         .ThenBy(i => lote[i].DocSacado, StringComparer.Ordinal)
                         .ThenBy(i => lote[i].ValorNominal))
            {
                if (recusas[i] || pendencias[i]) continue;
                var r = lote[i];
                var contexto = contextoBase with
                {
                    TotalSelecionado = selecionadoTotal,
                    SacadosSelecionados = sacados,
                    CedentesSelecionados = cedentes
                };
                foreach (var (regra, validador) in regras.Where(x => x.Validador.EhConcentracao))
                    await Executar(i, r, regra, validador, contexto, motivos, recusas, pendencias, ct);
                if (recusas[i] || pendencias[i]) continue;
                try { liquidos[i] = precificacao.Calcular(r, dataBase.Valor, config.Precificacao); }
                catch (Exception ex)
                {
                    motivos[i].Add(new Motivo("PRECIFICACAO_INVALIDA", ex.Message));
                    pendencias[i] = true;
                    continue;
                }
                selecionados.Add(r);
                selecionadoTotal += r.ValorNominal;
                var sacado = Documentos.Raiz(r.DocSacado);
                var cedente = Documentos.Digitos(r.DocCedente);
                sacados[sacado] = sacados.GetValueOrDefault(sacado) + r.ValorNominal;
                cedentes[cedente] = cedentes.GetValueOrDefault(cedente) + r.ValorNominal;
            }

            if (confirmar)
                foreach (var r in selecionados) estoque.Adicionar(r);

            var resultados = Enumerable.Range(0, lote.Count).Select(i => new ResultadoRecebivel(
                lote[i].NumeroDocumento,
                pendencias[i] ? StatusRecebivel.PENDENTE_VALIDACAO : recusas[i] ? StatusRecebivel.RECUSADA : StatusRecebivel.APROVADA,
                lote[i].ValorNominal, liquidos[i], motivos[i])).ToArray();
            return new ResultadoLote(lote.Count,
                resultados.Count(x => x.Status == StatusRecebivel.APROVADA),
                resultados.Count(x => x.Status == StatusRecebivel.RECUSADA),
                resultados.Count(x => x.Status == StatusRecebivel.PENDENTE_VALIDACAO),
                resultados.Sum(x => x.ValorLiquido ?? 0), resultados);
        }
        finally { estoque.Exclusao.Release(); }
    }

    private static async Task Executar(int i, Recebivel r, RegraConfig regra, IValidador validador,
        ContextoValidacao contexto, List<Motivo>[] motivos, bool[] recusas, bool[] pendencias, CancellationToken ct)
    {
        ResultadoRegra resultado;
        try { resultado = await validador.Validar(r, regra.Parametros, contexto, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { resultado = ResultadoRegra.Indeterminada("Falha técnica ao executar a regra."); }
        if (resultado.Estado == EstadoRegra.Aprovada) return;
        motivos[i].Add(new Motivo(regra.Codigo, resultado.Mensagem ?? "Validação inconclusiva."));
        if (resultado.Estado == EstadoRegra.Recusada) recusas[i] = true;
        else pendencias[i] = true;
    }
}
