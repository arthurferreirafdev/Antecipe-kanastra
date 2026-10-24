using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class ValorMinimoValidator : ValidatorBase
{
    public ValorMinimoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var valorMinimo = Decimal("valor_minimo");
        var valor = Valor(parametros, ValidatorsEnum.ValorNominal);

        return TentarDecimal(valor, out var valorDecimal) && valorDecimal >= valorMinimo;
    }
}
