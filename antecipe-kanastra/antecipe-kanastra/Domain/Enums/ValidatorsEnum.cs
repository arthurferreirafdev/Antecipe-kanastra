using System.Text.Json.Serialization;

namespace antecipe_kanastra.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidatorsEnum
{
    NomeFundo,
    DocFundo,
    NomeCedente,
    DocCedente,
    NomeSacado,
    DocSacado,
    NuDocumento,
    TipoRecebivel,
    ValorNominal,
    DataVencimentoOriginal,
    DataAquisicao,
    SituacaoCadastralSacado,
    ConcentracaoSacado,
    ConcentracaoCedente
}