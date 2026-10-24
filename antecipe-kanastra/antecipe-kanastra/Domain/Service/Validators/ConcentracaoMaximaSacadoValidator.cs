using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class ConcentracaoMaximaSacadoValidator : ValidatorBase
{
    public ConcentracaoMaximaSacadoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var percentualMaximo = Decimal("percentual_maximo");
        var raiz = Valor(parametros, ValidatorsEnum.ConcentracaoSacado);

        if (string.IsNullOrWhiteSpace(raiz))
        {
            return false;
        }

        var valor = Valor(parametros, ValidatorsEnum.ValorNominal);
        if (!TentarDecimal(valor, out var valorDecimal) || valorDecimal <= 0)
        {
            return false;
        }

        var totalCarteira = ObterTotalCarteira();
        if (totalCarteira <= 0)
        {
            return true;
        }

        var concentracaoAtual = (valorDecimal / totalCarteira) * 100m;
        return concentracaoAtual <= percentualMaximo;
    }

    protected decimal ObterTotalCarteira() => 1_000_000m;
}
