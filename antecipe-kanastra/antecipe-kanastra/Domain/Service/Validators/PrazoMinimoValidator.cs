using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class PrazoMinimoValidator : ValidatorBase
{
    public PrazoMinimoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var prazoMinimo = Inteiro("prazo_minimo_dias");
        var vencimento = Valor(parametros, ValidatorsEnum.DataVencimentoOriginal);
        var aquisicao = Valor(parametros, ValidatorsEnum.DataAquisicao);

        if (!TentarData(vencimento, out var dataVencimento) ||
            !TentarData(aquisicao, out var dataAquisicao))
        {
            return false;
        }

        var dias = (dataVencimento.Date - dataAquisicao.Date).TotalDays;
        if (dias < 0)
        {
            return false;
        }

        return dias >= prazoMinimo;
    }
}
