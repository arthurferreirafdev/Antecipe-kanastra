using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class PrazoMaximoValidator : ValidatorBase
{
    public PrazoMaximoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        var prazoMaximo = Inteiro("prazo_maximo_dias");
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

        return dias <= prazoMaximo;
    }
}
