using antecipe_kanastra.Domain.Services.Validadores;
using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class CnpjSacadoValidoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoCnpjValido()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "92.234.400/0001-80");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoCnpjValidoComFormatacao()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "04.252.011/0001-10");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoCnpjInvalido()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "11.111.111/1111-11");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoCnpjVazio()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoCnpjNulo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: null);
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoDigitosIguais()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CNPJ_SACADO_VALIDO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "00.000.000/0000-00");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
