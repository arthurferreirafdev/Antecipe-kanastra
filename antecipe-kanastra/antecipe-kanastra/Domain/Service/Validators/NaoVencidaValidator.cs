using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class NaoVencidaValidator : ValidatorBase
{
    public NaoVencidaValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var vencimento = Valor(parametros, ValidatorsEnum.DataVencimentoOriginal);
        if (!TentarData(vencimento, out var dataVencimento))
        {
            return false;
        }

        var hoje = DateTime.Today;
        return dataVencimento.Date >= hoje;
    }
}
