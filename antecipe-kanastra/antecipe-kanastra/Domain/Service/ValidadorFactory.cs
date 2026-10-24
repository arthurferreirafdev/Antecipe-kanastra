using antecipe_kanastra.Domain.Abstract;
using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Service;

public sealed class ValidadorFactory
{
    private readonly IReadOnlyDictionary<string, Func<ValidadorConfig, IValidator>> _criadores;

    public ValidadorFactory()
    {
        _criadores = new Dictionary<string, Func<ValidadorConfig, IValidator>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CNPJ_SACADO_VALIDO"] = config => new Validators.CnpjSacadoValidoValidator(config),
            ["SITUACAO_CADASTRAL_SACADO"] = config => new Validators.SituacaoCadastralSacadoValidator(config),
            ["TIPO_RECEBIVEL"] = config => new Validators.TipoRecebivelValidator(config),
            ["VALOR_MINIMO"] = config => new Validators.ValorMinimoValidator(config),
            ["NAO_VENCIDA"] = config => new Validators.NaoVencidaValidator(config),
            ["PRAZO_MAXIMO"] = config => new Validators.PrazoMaximoValidator(config),
            ["PRAZO_MINIMO"] = config => new Validators.PrazoMinimoValidator(config),
            ["CONCENTRACAO_MAXIMA_SACADO"] = config => new Validators.ConcentracaoMaximaSacadoValidator(config),
            ["CONCENTRACAO_MAXIMA_CEDENTE"] = config => new Validators.ConcentracaoMaximaCedenteValidator(config),
        };
    }

    public IValidator Criar(ValidadorConfig config)
    {
        if (_criadores.TryGetValue(config.Codigo, out var criador))
        {
            return criador(config);
        }

        throw new NotSupportedException($"Validador não suportado: {config.Codigo}");
    }
}
