using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Services.Validadores;
using Antecipe.Kanastra.Tests.Support;

namespace Antecipe.Kanastra.Tests.Validadores;

public class ValidacaoTests
{
    [Fact]
    public void DeveValidarAprovado_QuandoTodosValidadoresPassam()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var strategy = new ValidadorStrategy();
        var validadores = config.Validadores
            .Select(v => strategy.Obter(v))
            .ToList();
        var validacao = new Validacao(validadores);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao();
        
        var resultado = validacao.Validar(parametros);
        
        Assert.True(resultado);
    }

    [Fact]
    public void DeveValidarReprovado_QuandoAlgumValidadorFalha()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var strategy = new ValidadorStrategy();
        var validadores = config.Validadores
            .Select(v => strategy.Obter(v))
            .ToList();
        var validacao = new Validacao(validadores);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao(docSacado: "11.111.111/1111-11");
        
        var resultado = validacao.Validar(parametros);
        
        Assert.False(resultado);
    }

    [Fact]
    public void DeveValidarApenasValidadoresAtivos()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var strategy = new ValidadorStrategy();
        var validadores = config.Validadores
            .Select(v => strategy.Obter(v))
            .ToList();
        var validacao = new Validacao(validadores);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao();
        var detalhado = validacao.ValidarDetalhado(parametros);
        
        Assert.Contains(detalhado, d => d.Codigo == "CNPJ_SACADO_VALIDO" && d.Valido);
        Assert.DoesNotContain(detalhado, d => d.Codigo == "PRAZO_MINIMO");
        Assert.DoesNotContain(detalhado, d => d.Codigo == "CONCENTRACAO_MAXIMA_CEDENTE");
    }

    [Fact]
    public void DeveRetornarDetalhadoPorValidador()
    {
        var config = ValidatorsJsonLoader.Carregar();
        var strategy = new ValidadorStrategy();
        var validadores = config.Validadores
            .Select(v => strategy.Obter(v))
            .ToList();
        var validacao = new Validacao(validadores);
        var parametros = ValidatorsJsonLoader.ParametrosPadrao();
        var detalhado = validacao.ValidarDetalhado(parametros);
        
        Assert.NotEmpty(detalhado);
        Assert.All(detalhado, d => Assert.True(d.Valido));
    }
}
