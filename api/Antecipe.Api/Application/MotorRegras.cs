using Antecipe.Api.Domain;
using Antecipe.Api.Infrastructure;

namespace Antecipe.Api.Application;

public sealed class MotorRegras(ConfiguracaoService configuracao, EstoqueService estoque,
    PrecificacaoService precificacao)
{
    public async Task<ResultadoLote> Processar(IReadOnlyList<Recebivel> lote, bool confirmar, CancellationToken ct)
    {
        var config = configuracao.Carregar();
        var regras = config.Validadores.Where(r => r.Ativo)
            .Select(r => (Config: r, Validador: configuracao.Resolver(r.Codigo))).ToArray();
        await estoque.Exclusao.WaitAsync(ct);
        try
        {
            // Cada nota é avaliada contra a carteira projetada do próprio fundo (estoque + todas as notas do lote
            // desse fundo). O resultado não depende da ordem das linhas e uma recusa não interrompe as demais.
            var fundos = lote.GroupBy(r => Documentos.Digitos(r.DocFundo), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g =>
                {
                    var atual = estoque.Snapshot(g.Key);
                    return (Estoque: atual, Projetada: Projetar(atual, g));
                }, StringComparer.Ordinal);
            var motivos = Enumerable.Range(0, lote.Count).Select(_ => new List<Motivo>()).ToArray();
            var recusas = new bool[lote.Count];
            var pendencias = new bool[lote.Count];
            var duplicadas = lote.GroupBy(Documentos.Chave, StringComparer.Ordinal)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
            var contextos = new ContextoValidacao[lote.Count];

            for (var i = 0; i < lote.Count; i++)
            {
                var r = lote[i];
                var fundo = fundos[Documentos.Digitos(r.DocFundo)];
                contextos[i] = new ContextoValidacao(r.DataAquisicao, fundo.Projetada);
                if (fundo.Estoque.Chaves.Contains(Documentos.Chave(r)))
                {
                    motivos[i].Add(new Motivo("RECEBIVEL_DUPLICADO", "Esta nota já faz parte da carteira do fundo e não pode ser antecipada novamente."));
                    recusas[i] = true;
                }
                else if (duplicadas.Contains(Documentos.Chave(r)))
                {
                    motivos[i].Add(new Motivo("RECEBIVEL_DUPLICADO", "Esta nota aparece mais de uma vez no lote enviado."));
                    recusas[i] = true;
                }
                foreach (var (regra, validador) in regras.Where(x => !x.Validador.EhConcentracao))
                    await Executar(i, r, regra, validador, contextos[i], motivos, recusas, pendencias, ct);
            }

            // Concentração só é avaliada para notas que passaram nas demais regras.
            var liquidos = new decimal?[lote.Count];
            var selecionados = new List<Recebivel>();
            for (var i = 0; i < lote.Count; i++)
            {
                if (recusas[i] || pendencias[i]) continue;
                var r = lote[i];
                foreach (var (regra, validador) in regras.Where(x => x.Validador.EhConcentracao))
                    await Executar(i, r, regra, validador, contextos[i], motivos, recusas, pendencias, ct);
                if (recusas[i] || pendencias[i]) continue;
                try { liquidos[i] = precificacao.Calcular(r, r.DataAquisicao, config.Precificacao); }
                catch (Exception)
                {
                    motivos[i].Add(new Motivo("PRECIFICACAO_INVALIDA", "O desconto da antecipação ficaria maior que o valor da nota, por isso não foi possível calcular quanto você receberia."));
                    pendencias[i] = true;
                    continue;
                }
                selecionados.Add(r);
            }

            if (confirmar)
                foreach (var r in selecionados) estoque.Adicionar(r);

            return Consolidar(Enumerable.Range(0, lote.Count).Select(i => new ResultadoRecebivel(
                lote[i].NumeroDocumento,
                // Uma recusa definitiva prevalece sobre verificações que não puderam ser concluídas.
                recusas[i] ? StatusRecebivel.RECUSADA : pendencias[i] ? StatusRecebivel.PENDENTE_VALIDACAO : StatusRecebivel.APROVADA,
                lote[i].ValorNominal, liquidos[i], motivos[i])).ToArray());
        }
        finally { estoque.Exclusao.Release(); }
    }

    private static CarteiraSnapshot Projetar(CarteiraSnapshot estoque, IEnumerable<Recebivel> notas)
    {
        var sacados = new Dictionary<string, decimal>(estoque.PorSacado, StringComparer.Ordinal);
        var cedentes = new Dictionary<string, decimal>(estoque.PorCedente, StringComparer.Ordinal);
        var total = estoque.Total;
        foreach (var r in notas)
        {
            total += r.ValorNominal;
            var sacado = Documentos.Raiz(r.DocSacado);
            var cedente = Documentos.Digitos(r.DocCedente);
            sacados[sacado] = sacados.GetValueOrDefault(sacado) + r.ValorNominal;
            cedentes[cedente] = cedentes.GetValueOrDefault(cedente) + r.ValorNominal;
        }
        return new CarteiraSnapshot(total, sacados, cedentes, estoque.Chaves);
    }

    // Linhas com erro de formato são recusadas sem passar pelas regras; as demais seguem o fluxo normal.
    public async Task<ResultadoLote> Processar(IReadOnlyList<LinhaCsv> linhas, bool confirmar, CancellationToken ct)
    {
        var validas = linhas.Where(l => l.Recebivel is not null).ToArray();
        var lote = await Processar(validas.Select(l => l.Recebivel!).ToArray(), confirmar, ct);
        var invalidas = linhas.Where(l => l.Recebivel is null).Select(l => new ResultadoRecebivel(
            l.Dados.GetValueOrDefault("NU_DOCUMENTO", ""), StatusRecebivel.RECUSADA, 0, null, l.Erros)
        { Linha = l.Linha, Dados = l.Dados });
        return Consolidar(validas.Select((l, i) => lote.Resultados[i] with { Linha = l.Linha, Dados = l.Dados })
            .Concat(invalidas).OrderBy(x => x.Linha).ToArray());
    }

    private static ResultadoLote Consolidar(ResultadoRecebivel[] resultados) => new(resultados.Length,
        resultados.Count(x => x.Status == StatusRecebivel.APROVADA),
        resultados.Count(x => x.Status == StatusRecebivel.RECUSADA),
        resultados.Count(x => x.Status == StatusRecebivel.PENDENTE_VALIDACAO),
        resultados.Sum(x => x.ValorLiquido ?? 0), resultados);

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
