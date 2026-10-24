using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class ValorMinimoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoValorIgualAoMinimo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "VALOR_MINIMO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "100.00");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoValorMaiorQueMinimo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "VALOR_MINIMO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "1000.50");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoValorMenorQueMinimo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "VALOR_MINIMO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "99.99");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoValorComFormatoBrasileiro()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "VALOR_MINIMO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "150,50");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }
}
