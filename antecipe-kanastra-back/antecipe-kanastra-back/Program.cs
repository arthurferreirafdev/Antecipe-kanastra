using antecipe_kanastra.Application;
using antecipe_kanastra.Domain;
using antecipe_kanastra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddOperationTransformer<CsvUploadOpenApiTransformer>());
builder.Services.AddSingleton<EstoqueService>();
builder.Services.AddSingleton<PrecificacaoService>();
builder.Services.AddSingleton<ISituacaoCadastralProvider, SituacaoCadastralSimulada>();
builder.Services.AddSingleton<IValidador, CnpjSacadoValidador>();
builder.Services.AddSingleton<IValidador, SituacaoCadastralValidador>();
builder.Services.AddSingleton<IValidador, TipoRecebivelValidador>();
builder.Services.AddSingleton<IValidador, ValorMinimoValidador>();
builder.Services.AddSingleton<IValidador, NaoVencidaValidador>();
builder.Services.AddSingleton<IValidador, PrazoMaximoValidador>();
builder.Services.AddSingleton<IValidador, PrazoMinimoValidador>();
builder.Services.AddSingleton<IValidador, ConcentracaoSacadoValidador>();
builder.Services.AddSingleton<IValidador, ConcentracaoCedenteValidador>();
builder.Services.AddSingleton<ConfiguracaoService>();
builder.Services.AddScoped(_ => new DataBaseOperacao(DateOnly.FromDateTime(DateTime.Today)));
builder.Services.AddScoped<MotorRegras>();

var app = builder.Build();

// Resolve as dependências na inicialização para detectar configuração ou estoque inválido cedo.
_ = app.Services.GetRequiredService<ConfiguracaoService>();
_ = app.Services.GetRequiredService<EstoqueService>();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Antecipe API v1"));
app.MapControllers();

app.Run();

public partial class Program;
