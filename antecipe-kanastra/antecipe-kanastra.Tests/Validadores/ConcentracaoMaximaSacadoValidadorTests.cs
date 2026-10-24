using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class ConcentracaoMaximaSacadoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoConcentracaoAbaixoDoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "50000.00", concentracaoSacado: "92.234.400/0001-80");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoConcentracaoIgualAoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "100000.00", concentracaoSacado: "92.234.400/0001-80");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoConcentracaoAcimaDoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "100001.00", concentracaoSacado: "92.234.400/0001-80");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoRaizVazia()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "50000.00", concentracaoSacado: "");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoValorInvalido()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "0", concentracaoSacado: "92.234.400/0001-80");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
