using System.Globalization;
using System.Text.Json;
using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Services.Validadores;

public abstract class ValidadorBase : IValidador
{
    protected ValidadorBase(ValidadorConfig config)
    {
        Config = config;
    }

    protected ValidadorConfig Config { get; }

    public string Codigo => Config.Codigo;

    public bool Ativo => Config.Ativo;

    public bool Validar(List<Parametros> parametros)
    {
        if (!Ativo) return true;
        return ValidarRegra(parametros);
    }

    protected abstract bool ValidarRegra(List<Parametros> parametros);

    protected string? ObterValor(List<Parametros> parametros, ValidatorsEnum codigo)
        => parametros.FirstOrDefault(p => p.Codigo == codigo)?.Value;

    protected IReadOnlyList<string> ObterLista(string nome)
    {
        if (!Config.Parametros.TryGetValue(nome, out var elemento) ||
            elemento.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return elemento.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToList();
    }

    protected decimal ObterDecimal(string nome)
    {
        return Config.Parametros.TryGetValue(nome, out var elemento) &&
               elemento.TryGetDecimal(out var valor)
            ? valor
            : 0m;
    }

    protected int ObterInteiro(string nome)
    {
        return Config.Parametros.TryGetValue(nome, out var elemento) &&
               elemento.TryGetInt32(out var valor)
            ? valor
            : 0;
    }

    protected static bool TentarConverterDecimal(string? texto, out decimal valor)
        => decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) ||
           decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out valor);

    protected static bool TentarConverterData(string? texto, out DateTime data)
        => DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out data) ||
           DateTime.TryParse(texto, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out data);
}
