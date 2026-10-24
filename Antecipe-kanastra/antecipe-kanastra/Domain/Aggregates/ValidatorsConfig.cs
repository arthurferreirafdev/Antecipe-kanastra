using System.Text.Json;
using System.Text.Json.Serialization;

namespace antecipe_kanastra.Domain.Aggregates;

public sealed class ValidatorsConfig
{
    [JsonPropertyName("fundo")]
    public FundoConfig Fundo { get; init; } = new();

    [JsonPropertyName("validadores")]
    public List<ValidadorConfig> Validadores { get; init; } = [];

    [JsonPropertyName("precificacao")]
    public PrecificacaoConfig Precificacao { get; init; } = new();
}

public sealed class FundoConfig
{
    [JsonPropertyName("nome")]
    public string Nome { get; init; } = string.Empty;

    [JsonPropertyName("cnpj")]
    public string Cnpj { get; init; } = string.Empty;
}

public sealed class ValidadorConfig
{
    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("ativo")]
    public bool Ativo { get; init; }

    [JsonPropertyName("parametros")]
    public Dictionary<string, JsonElement> Parametros { get; init; } = [];
}

public sealed class PrecificacaoConfig
{
    [JsonPropertyName("metodo")]
    public string Metodo { get; init; } = string.Empty;

    [JsonPropertyName("taxa_mensal_percentual")]
    public decimal TaxaMensalPercentual { get; init; }

    [JsonPropertyName("base_dias")]
    public int BaseDias { get; init; }
}
