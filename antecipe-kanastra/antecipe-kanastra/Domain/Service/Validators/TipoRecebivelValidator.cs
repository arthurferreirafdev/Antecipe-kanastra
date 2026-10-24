using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class TipoRecebivelValidator : ValidatorBase
{
    public TipoRecebivelValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var aceitos = Lista("tipos_aceitos");
        if (aceitos.Count == 0)
        {
            return true;
        }

        var tipo = Valor(parametros, ValidatorsEnum.TipoRecebivel);
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return false;
        }

        return aceitos.Any(a => string.Equals(a.Trim(), tipo.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
