using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class NaoVencidaValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoVencimentoFuturo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "NAO_VENCIDA");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(vencimento: DateTime.Today.AddDays(10).ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoVencimentoHoje()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "NAO_VENCIDA");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(vencimento: DateTime.Today.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoVencimentoPassado()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "NAO_VENCIDA");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(vencimento: DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoDataInvalida()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "NAO_VENCIDA");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(vencimento: "data-invalida");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
