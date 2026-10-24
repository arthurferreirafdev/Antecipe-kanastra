using System.Text.Json;
using antecipe_kanastra.Domain;

namespace antecipe_kanastra.Application;

public sealed class ConfiguracaoService
{
    private readonly string _arquivo;
    private readonly IReadOnlyDictionary<string, IValidador> _validadores;

    public ConfiguracaoService(IWebHostEnvironment ambiente, IEnumerable<IValidador> validadores)
    {
        var arquivoLocal = Path.Combine(ambiente.ContentRootPath, "validators.json");
        var arquivoRepositorio = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath,
            "..", "..", "antecipe-kanastra", "antecipe-kanastra", "validators.json"));
        _arquivo = Environment.GetEnvironmentVariable("ANTECIPE_CONFIG")
            ?? (File.Exists(arquivoLocal) ? arquivoLocal : File.Exists(arquivoRepositorio)
                ? arquivoRepositorio : Path.Combine(AppContext.BaseDirectory, "validators.json"));
        _validadores = validadores.ToDictionary(v => v.Codigo, StringComparer.Ordinal);
        _ = Carregar(); // Falha no startup se a configuração inicial estiver incorreta.
    }

    public ConfiguracaoFundo Carregar()
    {
        // Cada chamada usa um snapshot completo. Uma alteração inválida gera erro explícito
        // e não substitui parâmetros parcialmente durante o processamento do lote.
        var config = JsonSerializer.Deserialize<ConfiguracaoFundo>(File.ReadAllText(_arquivo),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        if (config?.Fundo is null || config.Validadores is null || config.Precificacao is null ||
            string.IsNullOrWhiteSpace(config.Fundo.Nome) || Documentos.Digitos(config.Fundo.Cnpj).Length != 14)
            throw new ConfiguracaoException("Fundo ou configuração ausente/inválida.");

        var vistos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var regra in config.Validadores)
        {
            if (string.IsNullOrWhiteSpace(regra.Codigo) || !vistos.Add(regra.Codigo))
                throw new ConfiguracaoException("Código de regra ausente ou repetido.");
            if (regra.Parametros.ValueKind != JsonValueKind.Object)
                throw new ConfiguracaoException($"Parâmetros inválidos em {regra.Codigo}.");
            if (!_validadores.TryGetValue(regra.Codigo, out var validador))
            {
                if (regra.Ativo) throw new ConfiguracaoException($"Regra ativa desconhecida: {regra.Codigo}.");
                continue;
            }
            validador.ValidarParametros(regra.Parametros);
        }
        if (config.Precificacao.Metodo != "DESCONTO_SIMPLES" ||
            config.Precificacao.BaseDias <= 0 ||
            config.Precificacao.TaxaMensalPercentual < 0)
            throw new ConfiguracaoException("Método ou parâmetros de precificação inválidos.");
        return config;
    }

    public IValidador Resolver(string codigo) => _validadores.TryGetValue(codigo, out var v)
        ? v : throw new ConfiguracaoException($"Regra ativa desconhecida: {codigo}.");
}

public sealed class ConfiguracaoException(string mensagem) : Exception(mensagem);

public static class ParametrosRegra
{
    public static decimal Decimal(JsonElement p, string nome, decimal minimo, decimal maximo)
    {
        if (!p.TryGetProperty(nome, out var valor) || !valor.TryGetDecimal(out var numero) || numero < minimo || numero > maximo)
            throw new ConfiguracaoException($"Parâmetro {nome} inválido.");
        return numero;
    }

    public static int Inteiro(JsonElement p, string nome, int minimo)
    {
        if (!p.TryGetProperty(nome, out var valor) || !valor.TryGetInt32(out var numero) || numero < minimo)
            throw new ConfiguracaoException($"Parâmetro {nome} inválido.");
        return numero;
    }

    public static string Texto(JsonElement p, string nome)
    {
        if (!p.TryGetProperty(nome, out var valor) || valor.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(valor.GetString()))
            throw new ConfiguracaoException($"Parâmetro {nome} inválido.");
        return valor.GetString()!;
    }

    public static string[] Lista(JsonElement p, string nome)
    {
        if (!p.TryGetProperty(nome, out var valor) || valor.ValueKind != JsonValueKind.Array)
            throw new ConfiguracaoException($"Parâmetro {nome} inválido.");
        var itens = valor.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null).ToArray();
        if (itens.Length == 0 || itens.Any(string.IsNullOrWhiteSpace))
            throw new ConfiguracaoException($"Parâmetro {nome} inválido.");
        return itens!;
    }
}
