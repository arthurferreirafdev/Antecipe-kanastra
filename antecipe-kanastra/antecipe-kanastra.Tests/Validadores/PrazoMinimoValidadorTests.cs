using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class PrazoMinimoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoPrazoMaiorOuIgual30Dias_SeAtivo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "PRAZO_MINIMO");
        validadorConfig.Ativo = true;
        validadorConfig.Parametros["prazo_minimo_dias"] = System.Text.Json.JsonSerializer.SerializeToElement(30);
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(30);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoPrazoMenorQue30Dias_SeAtivo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "PRAZO_MINIMO");
        validadorConfig.Ativo = true;
        validadorConfig.Parametros["prazo_minimo_dias"] = System.Text.Json.JsonSerializer.SerializeToElement(30);
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(29);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoInativo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "PRAZO_MINIMO");
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(1);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }
}
