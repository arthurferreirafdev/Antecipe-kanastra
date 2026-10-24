using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public sealed class CnpjSacadoValidoValidator : ValidatorBase
{
    public CnpjSacadoValidoValidator(ValidadorConfig config) : base(config)
    {
    }

    protected override bool Validar(List<Parametros> parametros)
    {
        return CnpjValido(Valor(parametros, ValidatorsEnum.DocSacado));
    }

    private static bool CnpjValido(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            return false;
        }

        var digitos = new string(cnpj.Where(char.IsDigit).ToArray());

        if (digitos.Length != 14 || digitos.Distinct().Count() == 1)
        {
            return false;
        }

        int[] pesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var soma = 0;
        for (var i = 0; i < pesosPrimeiroDigito.Length; i++)
        {
            soma += (digitos[i] - '0') * pesosPrimeiroDigito[i];
        }

        var resto = soma % 11;
        var primeiroDigito = resto < 2 ? 0 : 11 - resto;

        if (digitos[12] - '0' != primeiroDigito)
        {
            return false;
        }

        soma = 0;
        for (var i = 0; i < pesosSegundoDigito.Length; i++)
        {
            soma += (digitos[i] - '0') * pesosSegundoDigito[i];
        }

        resto = soma % 11;
        var segundoDigito = resto < 2 ? 0 : 11 - resto;

        return digitos[13] - '0' == segundoDigito;
    }
}
