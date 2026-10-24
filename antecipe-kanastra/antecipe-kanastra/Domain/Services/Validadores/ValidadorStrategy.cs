using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Services.Validadores;

public sealed class ValidadorStrategy
{
    private readonly IReadOnlyDictionary<string, Func<ValidadorConfig, IValidador>> _estrategias;

    public ValidadorStrategy()
    {
        _estrategias = new Dictionary<string, Func<ValidadorConfig, IValidador>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CNPJ_SACADO_VALIDO"] = config => new CnpjSacadoValidoValidador(config),
            ["SITUACAO_CADASTRAL_SACADO"] = config => new SituacaoCadastralSacadoValidador(config),
            ["TIPO_RECEBIVEL"] = config => new TipoRecebivelValidador(config),
            ["VALOR_MINIMO"] = config => new ValorMinimoValidador(config),
            ["NAO_VENCIDA"] = config => new NaoVencidaValidador(config),
            ["PRAZO_MAXIMO"] = config => new PrazoMaximoValidador(config),
            ["PRAZO_MINIMO"] = config => new PrazoMinimoValidador(config),
            ["CONCENTRACAO_MAXIMA_SACADO"] = config => new ConcentracaoMaximaSacadoValidador(config),
            ["CONCENTRACAO_MAXIMA_CEDENTE"] = config => new ConcentracaoMaximaCedenteValidador(config),
        };
    }

    public IValidador Obter(ValidadorConfig config)
    {
        if (_estrategias.TryGetValue(config.Codigo, out var estrategia))
        {
            return estrategia(config);
        }

        throw new NotSupportedException($"Validador não suportado: {config.Codigo}");
    }
}
