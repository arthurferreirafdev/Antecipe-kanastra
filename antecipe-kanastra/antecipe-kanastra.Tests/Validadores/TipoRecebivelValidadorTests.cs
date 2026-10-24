using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class TipoRecebivelValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoDuplicata()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "TIPO_RECEBIVEL");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(tipo: "Duplicata");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoDuplicataDeServico()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "TIPO_RECEBIVEL");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(tipo: "Duplicata de Serviço");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoCaseInsensitive()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "TIPO_RECEBIVEL");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(tipo: "duplicata");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoTipoNaoAceito()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "TIPO_RECEBIVEL");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(tipo: "Cheque");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoTipoVazio()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "TIPO_RECEBIVEL");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(tipo: "");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
