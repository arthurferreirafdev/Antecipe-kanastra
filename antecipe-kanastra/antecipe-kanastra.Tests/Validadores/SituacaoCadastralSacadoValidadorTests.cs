using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class SituacaoCadastralSacadoValidadorTests
{
    [Fact]
    public void DeveRetornarVerdadeiro_QuandoSituacaoAtiva()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "SITUACAO_CADASTRAL_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(situacao: "ATIVA");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoSituacaoAtivaMaiusculoMinusculo()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "SITUACAO_CADASTRAL_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(situacao: "ativa");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoSituacaoInativa()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "SITUACAO_CADASTRAL_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(situacao: "INATIVA");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarFalso_QuandoSituacaoVazia()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validador = ValidatorsJsonLoader.CriarValidador(config, "SITUACAO_CADASTRAL_SACADO");
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(situacao: "");
        
        var resultado = validador.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveRetornarVerdadeiro_QuandoNaoHaSituacoesAceitas()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var validadorConfig = config.Validadores.First(v => v.Codigo == "SITUACAO_CADASTRAL_SACADO");
        validadorConfig.Parametros.Clear();
        var strategy = new antecipe_kanastra.Domain.Services.Validadores.ValidadorStrategy();
        var validador = strategy.Obter(validadorConfig);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(situacao: "QUALQUER");
        
        var resultado = validador.Validar(parametros);
        
        Assert.True(resultado);
    }
}
