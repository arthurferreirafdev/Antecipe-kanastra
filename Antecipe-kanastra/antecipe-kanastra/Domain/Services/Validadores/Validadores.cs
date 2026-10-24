using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Services.Validadores;

public sealed class CnpjSacadoValidoValidador : ValidadorBase
{
    public CnpjSacadoValidoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        return CnpjValido(ObterValor(parametros, ValidatorsEnum.DocSacado));
    }

    private static bool CnpjValido(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;
        var digitos = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digitos.Length != 14 || digitos.Distinct().Count() == 1) return false;
        int[] pesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var soma = 0;
        for (var i = 0; i < pesosPrimeiroDigito.Length; i++) soma += (digitos[i] - '0') * pesosPrimeiroDigito[i];
        var resto = soma % 11;
        var primeiroDigito = resto < 2 ? 0 : 11 - resto;
        if (digitos[12] - '0' != primeiroDigito) return false;
        soma = 0;
        for (var i = 0; i < pesosSegundoDigito.Length; i++) soma += (digitos[i] - '0') * pesosSegundoDigito[i];
        resto = soma % 11;
        var segundoDigito = resto < 2 ? 0 : 11 - resto;
        return digitos[13] - '0' == segundoDigito;
    }
}

public sealed class SituacaoCadastralSacadoValidador : ValidadorBase
{
    public SituacaoCadastralSacadoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var aceitas = ObterLista("situacoes_aceitas");
        if (aceitas.Count == 0) return true;
        var situacao = ObterValor(parametros, ValidatorsEnum.SituacaoCadastralSacado);
        if (string.IsNullOrWhiteSpace(situacao)) return false;
        return aceitas.Any(a => string.Equals(a.Trim(), situacao.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class TipoRecebivelValidador : ValidadorBase
{
    public TipoRecebivelValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var aceitos = ObterLista("tipos_aceitos");
        if (aceitos.Count == 0) return true;
        var tipo = ObterValor(parametros, ValidatorsEnum.TipoRecebivel);
        if (string.IsNullOrWhiteSpace(tipo)) return false;
        return aceitos.Any(a => string.Equals(a.Trim(), tipo.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class ValorMinimoValidador : ValidadorBase
{
    public ValorMinimoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var valorMinimo = ObterDecimal("valor_minimo");
        var valor = ObterValor(parametros, ValidatorsEnum.ValorNominal);
        return TentarConverterDecimal(valor, out var valorDecimal) && valorDecimal >= valorMinimo;
    }
}

public sealed class NaoVencidaValidador : ValidadorBase
{
    public NaoVencidaValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var vencimento = ObterValor(parametros, ValidatorsEnum.DataVencimentoOriginal);
        if (!TentarConverterData(vencimento, out var dataVencimento)) return false;
        return dataVencimento.Date >= DateTime.Today;
    }
}

public sealed class PrazoMaximoValidador : ValidadorBase
{
    public PrazoMaximoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var prazoMaximo = ObterInteiro("prazo_maximo_dias");
        var vencimento = ObterValor(parametros, ValidatorsEnum.DataVencimentoOriginal);
        var aquisicao = ObterValor(parametros, ValidatorsEnum.DataAquisicao);
        if (!TentarConverterData(vencimento, out var dataVencimento) ||
            !TentarConverterData(aquisicao, out var dataAquisicao)) return false;
        var dias = (dataVencimento.Date - dataAquisicao.Date).TotalDays;
        if (dias < 0) return false;
        return dias <= prazoMaximo;
    }
}

public sealed class PrazoMinimoValidador : ValidadorBase
{
    public PrazoMinimoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var prazoMinimo = ObterInteiro("prazo_minimo_dias");
        var vencimento = ObterValor(parametros, ValidatorsEnum.DataVencimentoOriginal);
        var aquisicao = ObterValor(parametros, ValidatorsEnum.DataAquisicao);
        if (!TentarConverterData(vencimento, out var dataVencimento) ||
            !TentarConverterData(aquisicao, out var dataAquisicao)) return false;
        var dias = (dataVencimento.Date - dataAquisicao.Date).TotalDays;
        if (dias < 0) return false;
        return dias >= prazoMinimo;
    }
}

public sealed class ConcentracaoMaximaSacadoValidador : ValidadorBase
{
    public ConcentracaoMaximaSacadoValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var percentualMaximo = ObterDecimal("percentual_maximo");
        var raiz = ObterValor(parametros, ValidatorsEnum.ConcentracaoSacado);
        if (string.IsNullOrWhiteSpace(raiz)) return false;
        var valor = ObterValor(parametros, ValidatorsEnum.ValorNominal);
        if (!TentarConverterDecimal(valor, out var valorDecimal) || valorDecimal <= 0) return false;
        var totalCarteira = ObterTotalCarteira();
        if (totalCarteira <= 0) return true;
        var concentracaoAtual = (valorDecimal / totalCarteira) * 100m;
        return concentracaoAtual <= percentualMaximo;
    }

    private decimal ObterTotalCarteira() => 1_000_000m;
}

public sealed class ConcentracaoMaximaCedenteValidador : ValidadorBase
{
    public ConcentracaoMaximaCedenteValidador(ValidadorConfig config) : base(config) { }

    protected override bool ValidarRegra(List<Parametros> parametros)
    {
        var percentualMaximo = ObterDecimal("percentual_maximo");
        var cedente = ObterValor(parametros, ValidatorsEnum.ConcentracaoCedente) ?? ObterValor(parametros, ValidatorsEnum.DocCedente);
        if (string.IsNullOrWhiteSpace(cedente)) return false;
        var valor = ObterValor(parametros, ValidatorsEnum.ValorNominal);
        if (!TentarConverterDecimal(valor, out var valorDecimal) || valorDecimal <= 0) return false;
        var totalCarteira = ObterTotalCarteira();
        if (totalCarteira <= 0) return true;
        var concentracaoAtual = (valorDecimal / totalCarteira) * 100m;
        return concentracaoAtual <= percentualMaximo;
    }

    private decimal ObterTotalCarteira() => 1_000_000m;
}
