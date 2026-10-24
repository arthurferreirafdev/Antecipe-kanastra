using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class SituacaoCadastralSacadoValidator : ValidatorBase
{
    public SituacaoCadastralSacadoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var aceitas = Lista("situacoes_aceitas");
        if (aceitas.Count == 0)
        {
            return true;
        }

        var situacao = Valor(parametros, ValidatorsEnum.SituacaoCadastralSacado);
        if (string.IsNullOrWhiteSpace(situacao))
        {
            return false;
        }

        return aceitas.Any(a => string.Equals(a.Trim(), situacao.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
