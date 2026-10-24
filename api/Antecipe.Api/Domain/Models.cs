using System.Text.Json;
using System.Text.Json.Serialization;

namespace Antecipe.Api.Domain;

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

// Linha lida do CSV: Recebivel é nulo quando a linha tem erro de formato, descrito em Erros.
public sealed record LinhaCsv(int Linha, IReadOnlyDictionary<string, string> Dados,
    Recebivel? Recebivel, IReadOnlyList<Motivo> Erros);

public sealed record ResultadoRecebivel(
    string NumeroDocumento, StatusRecebivel Status, decimal ValorNominal,
    decimal? ValorLiquido, IReadOnlyList<Motivo> Motivos)
{
    public int? Linha { get; init; }
    public IReadOnlyDictionary<string, string>? Dados { get; init; }
}

public sealed record ResultadoLote(
    int TotalRecebiveis, int QuantidadeAprovada, int QuantidadeRecusada,
    int QuantidadePendente, decimal ValorTotalLiquido,
    IReadOnlyList<ResultadoRecebivel> Resultados)
{
    public IReadOnlyList<ResultadoRecebivel> Aceitas => Resultados.Where(x => x.Status == StatusRecebivel.APROVADA).ToList();
    public IReadOnlyList<ResultadoRecebivel> Recusadas => Resultados.Where(x => x.Status == StatusRecebivel.RECUSADA).ToList();
    // Repetições removidas do lote antes do processamento.
    public IReadOnlyList<NotaDuplicada> Duplicadas { get; init; } = [];
    // Identifica o lote no armazenamento em memória das recusadas.
    public Guid? LoteId { get; init; }
    // Só presentes quando houve recusadas ou duplicadas e restou ao menos uma nota aprovada.
    public string? PerguntaDownload { get; init; }
    public string? CsvSemRecusadas { get; init; }
}

// Linha repetida no CSV: LinhaOriginal é a primeira ocorrência, que foi mantida e processada.
public sealed record NotaDuplicada(int Linha, int LinhaOriginal, string NumeroDocumento,
    IReadOnlyDictionary<string, string> Dados, string Mensagem);

// Nota recusada guardada em memória após a análise de um lote.
public sealed record NotaRecusada(Guid LoteId, string NomeArquivo, DateTimeOffset AnalisadaEm,
    int? Linha, string NumeroDocumento, decimal ValorNominal,
    IReadOnlyDictionary<string, string>? Dados, IReadOnlyList<Motivo> Motivos);

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
    // Parcelas de um mesmo documento compartilham o número e diferem pelo vencimento, por isso ele faz parte da chave.
    public static string Chave(Recebivel r) => Chave(r.DocFundo, r.DocCedente, r.NumeroDocumento,
        r.DataVencimento.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture));
    public static string Chave(string? docFundo, string? docCedente, string numeroDocumento, string? vencimento) =>
        $"{Digitos(docFundo)}|{Digitos(docCedente)}|{numeroDocumento.Trim().ToUpperInvariant()}|{(vencimento ?? "").Trim()}";
    public static string Raiz(string? valor) => Digitos(valor)[..Math.Min(8, Digitos(valor).Length)];
}

// DataBase é a data de aquisição da nota. Carteira é a carteira projetada do fundo da nota:
// estoque já adquirido somado a todas as notas do lote desse fundo (inclusive a própria nota).
public sealed record ContextoValidacao(DateOnly DataBase, CarteiraSnapshot Carteira);

public sealed record CarteiraSnapshot(
    decimal Total, IReadOnlyDictionary<string, decimal> PorSacado,
    IReadOnlyDictionary<string, decimal> PorCedente, IReadOnlySet<string> Chaves);
