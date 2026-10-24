using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class PrazoMaximoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoPrazoMenorOuIgual180Dias()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "PRAZO_MAXIMO");
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(180);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoPrazoMaiorQue180Dias()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "PRAZO_MAXIMO");
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(181);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoVencimentoAnteriorAAquisicao()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "PRAZO_MAXIMO");
        var aquisicao = DateTime.Today;
        var vencimento = aquisicao.AddDays(-1);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: aquisicao.ToString("dd/MM/yyyy"),
            vencimento: vencimento.ToString("dd/MM/yyyy"));
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoDatasInvalidas()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "PRAZO_MAXIMO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(
            aquisicao: "xxx",
            vencimento: "yyy");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }
}
