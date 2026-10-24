using System.Globalization;
using System.Text.Json;
using antecipe_kanastra.Domain.Abstract;
using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service.Validators;

public abstract class ValidatorBase : IValidator
{
    protected ValidatorBase(ValidadorConfig config)
    {
        Config = config;
    }

    protected ValidadorConfig Config { get; }

    public string Codigo => Config.Codigo;

    public bool Ativo => Config.Ativo;

    public bool IsValid(List<Parametros> Parametros)
    {
        return !Ativo || Validar(Parametros);
    }

    protected abstract bool Validar(List<Parametros> parametros);

    protected string? Valor(List<Parametros> parametros, ValidatorsEnum codigo)
    {
        return parametros.FirstOrDefault(p => p.Codigo == codigo)?.Value;
    }

    protected IReadOnlyList<string> Lista(string nome)
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

    protected decimal Decimal(string nome)
    {
        return Config.Parametros.TryGetValue(nome, out var elemento) &&
               elemento.TryGetDecimal(out var valor)
            ? valor
            : 0m;
    }

    protected int Inteiro(string nome)
    {
        return Config.Parametros.TryGetValue(nome, out var elemento) &&
               elemento.TryGetInt32(out var valor)
            ? valor
            : 0;
    }

    protected static bool TentarDecimal(string? texto, out decimal valor)
    {
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) ||
               decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out valor);
    }

    protected static bool TentarData(string? texto, out DateTime data)
    {
        return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out data) ||
               DateTime.TryParse(texto, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out data);
    }
}
