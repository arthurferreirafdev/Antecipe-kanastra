using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class ConcentracaoMaximaCedenteValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoInativoPorPadrao()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "CONCENTRACAO_MAXIMA_CEDENTE");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "50000.00");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoAtivoEAbaixoDoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "CONCENTRACAO_MAXIMA_CEDENTE");
        validadorConfig.Ativo = true;
        validadorConfig.Parametros["percentual_maximo"] = System.Text.Json.JsonSerializer.SerializeToElement(8.0m);
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "50000.00", concentracaoCedente: "12.345.678/0001-90");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoAtivoEEIgualAoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "CONCENTRACAO_MAXIMA_CEDENTE");
        validadorConfig.Ativo = true;
        validadorConfig.Parametros["percentual_maximo"] = System.Text.Json.JsonSerializer.SerializeToElement(8.0m);
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "80000.00", concentracaoCedente: "12.345.678/0001-90");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoAtivoEAcimaDoLimite()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "CONCENTRACAO_MAXIMA_CEDENTE");
        validadorConfig.Ativo = true;
        validadorConfig.Parametros["percentual_maximo"] = System.Text.Json.JsonSerializer.SerializeToElement(8.0m);
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "80001.00", concentracaoCedente: "12.345.678/0001-90");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoAtivoECedenteVazio()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "CONCENTRACAO_MAXIMA_CEDENTE");
        validadorConfig.Ativo = true;
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(valor: "50000.00", concentracaoCedente: "");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
