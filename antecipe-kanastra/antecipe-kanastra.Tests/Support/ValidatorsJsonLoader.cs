using System.Text.Json;
using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;
using antecipe_kanastra.Domain.Services.Validadores;

namespace Antecipe.Kanastra.Tests.Support;

public static class ValidatorsJsonLoader
{
    public static ValidatorsConfig Carregar(string caminho = "validators.json")
    {
        var json = File.ReadAllText(caminho);
        var opcoes = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var config = JsonSerializer.Deserialize<ValidatorsConfig>(json, opcoes) ?? new ValidatorsConfig();
        return config;
    }

    public static List<Parametros> ParametrosPadrao(
        string? docSacado = "92.234.400/0001-80",
        string? situacao = "ATIVA",
        string? tipo = "Duplicata",
        string? valor = "1000.00",
        string? vencimento = null,
        string? aquisicao = null,
        string? concentracaoSacado = null,
        string? concentracaoCedente = null)
    {
        vencimento ??= DateTime.Today.AddDays(30).ToString("dd/MM/yyyy");
        aquisicao ??= DateTime.Today.ToString("dd/MM/yyyy");
        concentracaoSacado ??= docSacado ?? string.Empty;
        concentracaoCedente ??= "12.345.678/0001-90";

        return
        [
            new Parametros(ValidatorsEnum.DocSacado, docSacado ?? string.Empty),
            new Parametros(ValidatorsEnum.SituacaoCadastralSacado, situacao ?? string.Empty),
            new Parametros(ValidatorsEnum.TipoRecebivel, tipo ?? string.Empty),
            new Parametros(ValidatorsEnum.ValorNominal, valor ?? string.Empty),
            new Parametros(ValidatorsEnum.DataVencimentoOriginal, vencimento),
            new Parametros(ValidatorsEnum.DataAquisicao, aquisicao),
            new Parametros(ValidatorsEnum.ConcentracaoSacado, concentracaoSacado),
            new Parametros(ValidatorsEnum.ConcentracaoCedente, concentracaoCedente)
        ];
    }

    public static IValidador CriarValidador(ValidatorsConfig config, string codigo)
    {
        var validadorConfig = config.Validadores.First(v => v.Codigo == codigo);
        var strategy = new ValidadorStrategy();
        return strategy.Obter(validadorConfig);
    }
}
