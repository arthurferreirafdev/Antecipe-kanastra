using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Services.Validadores;

namespace Antecipe.Kanastra.Tests.Validadores;

public class ValidadorStrategyTests
{
    [Fact]
    public void DeveObterValidadorParaCadaCodigoSuportado()
    {
        var strategy = new ValidadorStrategy();
        var codigos = new[]
        {
            "CNPJ_SACADO_VALIDO",
            "SITUACAO_CADASTRAL_SACADO",
            "TIPO_RECEBIVEL",
            "VALOR_MINIMO",
            "NAO_VENCIDA",
            "PRAZO_MAXIMO",
            "PRAZO_MINIMO",
            "CONCENTRACAO_MAXIMA_SACADO",
            "CONCENTRACAO_MAXIMA_CEDENTE"
        };

        foreach (var codigo in codigos)
        {
            var config = new ValidadorConfig { Codigo = codigo, Ativo = true, Parametros = [] };
            var validador = strategy.Obter(config);
            Assert.NotNull(validador);
            Assert.Equal(codigo, validador.Codigo, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DeveLancarExcecao_QuandoCodigoNaoSuportado()
    {
        var strategy = new ValidadorStrategy();
        var config = new ValidadorConfig { Codigo = "NAO_EXISTE", Ativo = true, Parametros = [] };
        
        var excecao = Assert.Throws<NotSupportedException>(() => strategy.Obter(config));
        
        Assert.Contains("não suportado", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }
}
