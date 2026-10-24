using System.Text.Json;
using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Services.Validadores;

var caminho = Path.Combine(AppContext.BaseDirectory, "validadores_antecipe.json");
if (!File.Exists(caminho))
{
    caminho = Path.Combine(Directory.GetCurrentDirectory(), "validadores_antecipe.json");
}
if (!File.Exists(caminho))
{
    caminho = Path.Combine(Directory.GetCurrentDirectory(), "antecipe-kanastra", "validadores_antecipe.json");
}

var json = await File.ReadAllTextAsync(caminho);
var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var config = JsonSerializer.Deserialize<ValidatorsConfig>(json, opcoes) ?? new ValidatorsConfig();

var strategy = new ValidadorStrategy();
var validadores = config.Validadores.Select(v => strategy.Obter(v)).ToList();

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton(strategy);
builder.Services.AddSingleton<IReadOnlyList<IValidador>>(validadores);
builder.Services.AddSingleton<Validacao>(_ => new Validacao(validadores));

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "ok", validadores = validadores.Select(v => new { v.Codigo, v.Ativo }) }));

app.Run();
