using System.Text.Json;
using System.Globalization;
using antecipe_kanastra.Domain;

namespace antecipe_kanastra.Application;

public interface IValidador
{
    string Codigo { get; }
    bool EhConcentracao => false;
    void ValidarParametros(JsonElement parametros);
    ValueTask<ResultadoRegra> Validar(Recebivel recebivel, JsonElement parametros,
        ContextoValidacao contexto, CancellationToken cancellationToken);
}

public interface ISituacaoCadastralProvider
{
    ValueTask<string?> Consultar(string cnpjNormalizado, CancellationToken cancellationToken);
}

public sealed class SituacaoCadastralSimulada : ISituacaoCadastralProvider
{
    private readonly Dictionary<string, string> _situacoes;
    public SituacaoCadastralSimulada(IWebHostEnvironment ambiente)
    {
        var caminho = Path.Combine(ambiente.ContentRootPath, "situacoes_cadastrais_dev.json");
        _situacoes = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(caminho))!
            .ToDictionary(x => Documentos.Digitos(x.Key), x => x.Value, StringComparer.Ordinal);
    }
    public ValueTask<string?> Consultar(string cnpjNormalizado, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_situacoes.GetValueOrDefault(cnpjNormalizado));
    }
}

public sealed class CnpjSacadoValidador : IValidador
{
    public string Codigo => "CNPJ_SACADO_VALIDO";
    public void ValidarParametros(JsonElement p) { }
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
        => ValueTask.FromResult(Valido(r.DocSacado) ? ResultadoRegra.Aprovada() : ResultadoRegra.Recusada("CNPJ do sacado inválido."));

    public static bool Valido(string documento)
    {
        var d = Documentos.Digitos(documento);
        if (d.Length != 14 || d.All(x => x == d[0])) return false;
        int Digito(int tamanho)
        {
            var pesos = tamanho == 12 ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 } :
                new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var soma = Enumerable.Range(0, tamanho).Sum(i => (d[i] - '0') * pesos[i]);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return d[12] - '0' == Digito(12) && d[13] - '0' == Digito(13);
    }
}

public sealed class SituacaoCadastralValidador(ISituacaoCadastralProvider provider) : IValidador
{
    public string Codigo => "SITUACAO_CADASTRAL_SACADO";
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Lista(p, "situacoes_aceitas");
    public async ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        try
        {
            var situacao = await provider.Consultar(Documentos.Digitos(r.DocSacado), ct);
            if (string.IsNullOrWhiteSpace(situacao)) return ResultadoRegra.Indeterminada("Situação cadastral do sacado indisponível para verificação.");
            var aceitas = ParametrosRegra.Lista(p, "situacoes_aceitas");
            return aceitas.Contains(situacao, StringComparer.OrdinalIgnoreCase)
                ? ResultadoRegra.Aprovada()
                : ResultadoRegra.Recusada($"Situação cadastral do sacado: {situacao}. Aceitas: {string.Join(", ", aceitas)}.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return ResultadoRegra.Indeterminada("Falha ao consultar a situação cadastral do sacado."); }
    }
}

public sealed class TipoRecebivelValidador : IValidador
{
    public string Codigo => "TIPO_RECEBIVEL";
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Lista(p, "tipos_aceitos");
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var tipos = ParametrosRegra.Lista(p, "tipos_aceitos");
        return ValueTask.FromResult(tipos.Contains(r.TipoRecebivel, StringComparer.OrdinalIgnoreCase)
            ? ResultadoRegra.Aprovada() : ResultadoRegra.Recusada($"Tipo de recebível não aceito. Tipos aceitos: {string.Join(", ", tipos)}."));
    }
}

public sealed class ValorMinimoValidador : IValidador
{
    public string Codigo => "VALOR_MINIMO";
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Decimal(p, "valor_minimo", 0, decimal.MaxValue);
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var minimo = ParametrosRegra.Decimal(p, "valor_minimo", 0, decimal.MaxValue);
        return ValueTask.FromResult(r.ValorNominal >= minimo ? ResultadoRegra.Aprovada()
            : ResultadoRegra.Recusada($"O valor da nota está abaixo do mínimo de R$ {minimo.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))} permitido pelo fundo."));
    }
}

public sealed class NaoVencidaValidador : IValidador
{
    public string Codigo => "NAO_VENCIDA";
    public void ValidarParametros(JsonElement p) { }
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
        => ValueTask.FromResult(r.DataVencimento >= c.DataBase ? ResultadoRegra.Aprovada()
            : ResultadoRegra.Recusada($"A nota venceu em {r.DataVencimento:dd/MM/yyyy}, antes da data da análise ({c.DataBase:dd/MM/yyyy})."));
}

public sealed class PrazoMaximoValidador : IValidador
{
    public string Codigo => "PRAZO_MAXIMO";
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Inteiro(p, "prazo_maximo_dias", 0);
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var maximo = ParametrosRegra.Inteiro(p, "prazo_maximo_dias", 0);
        var dias = r.DataVencimento.DayNumber - c.DataBase.DayNumber;
        return ValueTask.FromResult(dias <= maximo ? ResultadoRegra.Aprovada()
            : ResultadoRegra.Recusada($"Prazo de {dias} dias excede o máximo de {maximo} dias."));
    }
}

public sealed class PrazoMinimoValidador : IValidador
{
    public string Codigo => "PRAZO_MINIMO";
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Inteiro(p, "prazo_minimo_dias", 0);
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var minimo = ParametrosRegra.Inteiro(p, "prazo_minimo_dias", 0);
        var dias = r.DataVencimento.DayNumber - c.DataBase.DayNumber;
        return ValueTask.FromResult(dias >= minimo ? ResultadoRegra.Aprovada()
            : ResultadoRegra.Recusada($"Prazo de {dias} dias abaixo do mínimo de {minimo} dias."));
    }
}

public sealed class ConcentracaoSacadoValidador : IValidador
{
    public string Codigo => "CONCENTRACAO_MAXIMA_SACADO";
    public bool EhConcentracao => true;
    public void ValidarParametros(JsonElement p)
    {
        _ = ParametrosRegra.Decimal(p, "percentual_maximo", 0, 100);
        if (ParametrosRegra.Texto(p, "agrupamento") != "RAIZ_CNPJ")
            throw new ConfiguracaoException("Agrupamento de sacado não suportado.");
    }
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var limite = ParametrosRegra.Decimal(p, "percentual_maximo", 0, 100);
        var chave = Documentos.Raiz(r.DocSacado);
        var grupo = c.Carteira.PorSacado.GetValueOrDefault(chave) +
            (c.SacadosSelecionados?.GetValueOrDefault(chave) ?? 0) + r.ValorNominal;
        return ValueTask.FromResult(Concentracao.Calcular(grupo, c.Carteira.Total + c.TotalSelecionado + r.ValorNominal, limite, "sacado"));
    }
}

public sealed class ConcentracaoCedenteValidador : IValidador
{
    public string Codigo => "CONCENTRACAO_MAXIMA_CEDENTE";
    public bool EhConcentracao => true;
    public void ValidarParametros(JsonElement p) => _ = ParametrosRegra.Decimal(p, "percentual_maximo", 0, 100);
    public ValueTask<ResultadoRegra> Validar(Recebivel r, JsonElement p, ContextoValidacao c, CancellationToken ct)
    {
        var limite = ParametrosRegra.Decimal(p, "percentual_maximo", 0, 100);
        var chave = Documentos.Digitos(r.DocCedente);
        var grupo = c.Carteira.PorCedente.GetValueOrDefault(chave) +
            (c.CedentesSelecionados?.GetValueOrDefault(chave) ?? 0) + r.ValorNominal;
        return ValueTask.FromResult(Concentracao.Calcular(grupo, c.Carteira.Total + c.TotalSelecionado + r.ValorNominal, limite, "cedente"));
    }
}

public static class Concentracao
{
    public static ResultadoRegra Calcular(decimal grupo, decimal carteiraProjetada, decimal limite, string tipo)
    {
        if (carteiraProjetada <= 0) return ResultadoRegra.Indeterminada("Carteira projetada sem valor positivo.");
        var percentual = grupo * 100 / carteiraProjetada;
        return percentual <= limite ? ResultadoRegra.Aprovada()
            : ResultadoRegra.Recusada($"Concentração projetada do {tipo}: {percentual.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}% (limite {limite.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%).");
    }
}
