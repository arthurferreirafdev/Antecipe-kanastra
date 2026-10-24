using System.Text.Json;
using System.Text.Json.Serialization;

namespace antecipe_kanastra.Domain;

public sealed record FundoConfig(string Nome, string Cnpj);
public sealed record RegraConfig(string Codigo, bool Ativo, JsonElement Parametros);
public sealed record PrecificacaoConfig(string Metodo, decimal TaxaMensalPercentual, int BaseDias);
public sealed record ConfiguracaoFundo(FundoConfig Fundo, List<RegraConfig> Validadores, PrecificacaoConfig Precificacao);

public sealed record Recebivel(
    string NomeFundo, string DocFundo, string NomeCedente, string DocCedente,
    string NomeSacado, string DocSacado, string NumeroDocumento, string TipoRecebivel,
    decimal ValorNominal, DateOnly DataVencimento, DateOnly DataAquisicao);

public sealed record Motivo(string Codigo, string Mensagem);

[JsonConverter(typeof(JsonStringEnumConverter<StatusRecebivel>))]
public enum StatusRecebivel { APROVADA, RECUSADA, PENDENTE_VALIDACAO }

public sealed record ResultadoRecebivel(
    string NumeroDocumento, StatusRecebivel Status, decimal ValorNominal,
    decimal? ValorLiquido, IReadOnlyList<Motivo> Motivos);

public sealed record ResultadoLote(
    int TotalRecebiveis, int QuantidadeAprovada, int QuantidadeRecusada,
    int QuantidadePendente, decimal ValorTotalLiquido,
    IReadOnlyList<ResultadoRecebivel> Resultados);

public enum EstadoRegra { Aprovada, Recusada, Indeterminada }
public sealed record ResultadoRegra(EstadoRegra Estado, string? Mensagem = null)
{
    public static ResultadoRegra Aprovada() => new(EstadoRegra.Aprovada);
    public static ResultadoRegra Recusada(string mensagem) => new(EstadoRegra.Recusada, mensagem);
    public static ResultadoRegra Indeterminada(string mensagem) => new(EstadoRegra.Indeterminada, mensagem);
}

public static class Documentos
{
    public static string Digitos(string? valor) => new((valor ?? "").Where(char.IsAsciiDigit).ToArray());
    public static string Chave(Recebivel r) => $"{Digitos(r.DocFundo)}|{Digitos(r.DocCedente)}|{r.NumeroDocumento.Trim().ToUpperInvariant()}";
    public static string Raiz(string? valor) => Digitos(valor)[..Math.Min(8, Digitos(valor).Length)];
}

public sealed record ContextoValidacao(DateOnly DataBase, CarteiraSnapshot Carteira,
    decimal TotalSelecionado = 0,
    IReadOnlyDictionary<string, decimal>? SacadosSelecionados = null,
    IReadOnlyDictionary<string, decimal>? CedentesSelecionados = null);

public sealed record DataBaseOperacao(DateOnly Valor);

public sealed record CarteiraSnapshot(
    decimal Total, IReadOnlyDictionary<string, decimal> PorSacado,
    IReadOnlyDictionary<string, decimal> PorCedente, IReadOnlySet<string> Chaves);
