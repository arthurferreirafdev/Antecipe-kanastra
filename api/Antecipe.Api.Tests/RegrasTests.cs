using System.Text.Json;
using System.Text;
using Antecipe.Api.Application;
using Antecipe.Api.Domain;
using Antecipe.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace AntecipeTests;

public sealed class RegrasTests
{
    private static readonly DateOnly Base = new(2026, 10, 22);
    private static readonly CarteiraSnapshot Vazia = new(0, new Dictionary<string, decimal>(),
        new Dictionary<string, decimal>(), new HashSet<string>());
    private static Recebivel Nota(string numero = "NF1", decimal valor = 100m, int dias = 30,
        string sacado = "58206002000111") => new("ANTECIPE FIDC", "92234400000180", "Cedente",
        "10062050000157", "Sacado", sacado, numero, "Duplicata", valor, Base.AddDays(dias), Base);
    private static JsonElement P(string json) => JsonDocument.Parse(json).RootElement.Clone();
    private static ContextoValidacao C(CarteiraSnapshot? carteira = null) => new(Base, carteira ?? Vazia);
    private static async Task<ResultadoRegra> Rodar(IValidador v, Recebivel r, string parametros = "{}")
        => await v.Validar(r, P(parametros), C(), CancellationToken.None);

    [Fact]
    public void CsvRespeitaSeparadorDecimalEDataDoExemplo()
    {
        const string cabecalho = "NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO\n";
        const string linha = "ANTECIPE FIDC;92.234.400/0001-80;Cedente;10.062.050/0001-57;Sacado;58.206.002/0001-11;NF1;Duplicata;100,50;22/11/2026;22/10/2026\n";
        static List<Recebivel> Ler(string csv) => CsvRecebiveis.Ler(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        var nota = Assert.Single(Ler(cabecalho + linha));
        Assert.Equal(100.50m, nota.ValorNominal);
        Assert.Equal(new DateOnly(2026, 11, 22), nota.DataVencimento);
        Assert.Throws<FormatException>(() => Ler("COLUNA;ERRADA\n" + linha));
        Assert.Throws<FormatException>(() => Ler(cabecalho + linha.Replace("100,50", "100.50")));
        Assert.Throws<FormatException>(() => Ler(cabecalho + linha.Replace("22/11/2026", "2026-11-22")));
    }

    [Theory]
    [InlineData("58.206.002/0001-11", true)]
    [InlineData("58206002000111", true)]
    [InlineData("58206002000112", false)]
    [InlineData("11.111.111/1111-11", false)]
    public void Cnpj(string cnpj, bool valido) => Assert.Equal(valido, CnpjSacadoValidador.Valido(cnpj));

    [Fact]
    public async Task SituacaoAceitaRecusadaEIndisponivel()
    {
        const string p = "{\"situacoes_aceitas\":[\"ATIVA\"]}";
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(new SituacaoCadastralValidador(new FakeSituacao("ATIVA")), Nota(), p)).Estado);
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(new SituacaoCadastralValidador(new FakeSituacao("BAIXADA")), Nota(), p)).Estado);
        Assert.Equal(EstadoRegra.Indeterminada, (await Rodar(new SituacaoCadastralValidador(new FakeSituacao(null)), Nota(), p)).Estado);
    }

    [Fact]
    public async Task TipoEValorMinimo()
    {
        var tipo = new TipoRecebivelValidador();
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(tipo, Nota(), "{\"tipos_aceitos\":[\"Duplicata\"]}")).Estado);
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(tipo, Nota() with { TipoRecebivel = "Cheque" }, "{\"tipos_aceitos\":[\"Duplicata\"]}")).Estado);
        var minimo = new ValorMinimoValidador();
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(minimo, Nota(valor: 100), "{\"valor_minimo\":100}")).Estado);
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(minimo, Nota(valor: 99.99m), "{\"valor_minimo\":100}")).Estado);
    }

    [Fact]
    public async Task VencimentoEPrazosNosLimites()
    {
        var vencida = new NaoVencidaValidador();
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(vencida, Nota(dias: -1))).Estado);
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(vencida, Nota(dias: 0))).Estado);
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(vencida, Nota(dias: 1))).Estado);
        var maximo = new PrazoMaximoValidador();
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(maximo, Nota(dias: 180), "{\"prazo_maximo_dias\":180}")).Estado);
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(maximo, Nota(dias: 181), "{\"prazo_maximo_dias\":180}")).Estado);
        var minimo = new PrazoMinimoValidador();
        Assert.Equal(EstadoRegra.Aprovada, (await Rodar(minimo, Nota(dias: 30), "{\"prazo_minimo_dias\":30}")).Estado);
        Assert.Equal(EstadoRegra.Recusada, (await Rodar(minimo, Nota(dias: 29), "{\"prazo_minimo_dias\":30}")).Estado);
    }

    [Fact]
    public async Task ConcentracaoAbaixoNoLimiteEAcima()
    {
        var v = new ConcentracaoSacadoValidador();
        // A carteira projetada já inclui a própria nota.
        var sacados = new Dictionary<string, decimal> { ["58206002"] = 149 };
        var carteira = new CarteiraSnapshot(1000, sacados,
            new Dictionary<string, decimal>(), new HashSet<string>());
        var p = P("{\"percentual_maximo\":15,\"agrupamento\":\"RAIZ_CNPJ\"}");
        Assert.Equal(EstadoRegra.Aprovada, (await v.Validar(Nota(valor: 100), p, C(carteira), default)).Estado);
        sacados["58206002"] = 150;
        Assert.Equal(EstadoRegra.Aprovada, (await v.Validar(Nota(valor: 100), p, C(carteira), default)).Estado);
        sacados["58206002"] = 151;
        Assert.Equal(EstadoRegra.Recusada, (await v.Validar(Nota(valor: 100), p, C(carteira), default)).Estado);
    }

    [Fact]
    public async Task DataBaseEhADataDeAquisicaoDaNota()
    {
        using var c = new Cenario("{\"codigo\":\"NAO_VENCIDA\",\"ativo\":true,\"parametros\":{}}," +
            "{\"codigo\":\"PRAZO_MAXIMO\",\"ativo\":true,\"parametros\":{\"prazo_maximo_dias\":180}}");
        var antiga = Nota("ANTIGA") with { DataAquisicao = new DateOnly(2025, 1, 1), DataVencimento = new DateOnly(2025, 6, 30) };
        var longa = antiga with { NumeroDocumento = "LONGA", DataVencimento = new DateOnly(2025, 7, 1) };
        var r = await c.Motor.Processar([antiga, longa], false, default);
        Assert.Equal(StatusRecebivel.APROVADA, r.Resultados[0].Status);
        Assert.Equal("PRAZO_MAXIMO", Assert.Single(r.Resultados[1].Motivos).Codigo);
    }

    [Fact]
    public void PrecificacaoSimplesEValorNegativo()
    {
        var s = new PrecificacaoService();
        Assert.Equal(98m, s.Calcular(Nota(valor: 100, dias: 30), Base, new("DESCONTO_SIMPLES", 2, 30)));
        Assert.Throws<InvalidOperationException>(() => s.Calcular(Nota(valor: 100, dias: 200), Base, new("DESCONTO_SIMPLES", 20, 30)));
    }

    private sealed class FakeSituacao(string? valor) : ISituacaoCadastralProvider
    {
        public ValueTask<string?> Consultar(string cnpj, CancellationToken ct) => ValueTask.FromResult(valor);
    }

    private sealed class Ambiente(string pasta) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = pasta;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = pasta;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(pasta);
    }

    private static readonly IValidador[] Validadores = [new CnpjSacadoValidador(),
        new SituacaoCadastralValidador(new FakeSituacao("ATIVA")), new TipoRecebivelValidador(),
        new ValorMinimoValidador(), new NaoVencidaValidador(), new PrazoMaximoValidador(),
        new PrazoMinimoValidador(), new ConcentracaoSacadoValidador(), new ConcentracaoCedenteValidador()];

    private sealed class Cenario : IDisposable
    {
        public string Pasta { get; } = Path.Combine(Path.GetTempPath(), "antecipe-tests-" + Guid.NewGuid().ToString("N"));
        public ConfiguracaoService Config { get; }
        public EstoqueService Estoque { get; }
        public MotorRegras Motor { get; }
        public string Dados => Path.Combine(Pasta, "Dados");
        public Cenario(string regras)
        {
            Directory.CreateDirectory(Dados);
            File.WriteAllText(Path.Combine(Dados, "validators.json"),
                "{\"fundo\":{\"nome\":\"ANTECIPE FIDC\",\"cnpj\":\"92.234.400/0001-80\"},\"validadores\":[" + regras +
                "],\"precificacao\":{\"metodo\":\"DESCONTO_SIMPLES\",\"taxa_mensal_percentual\":2,\"base_dias\":30}}");
            File.WriteAllText(Path.Combine(Dados, "estoque_antecipe.csv"),
                "NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO\n" +
                "ANTECIPE FIDC;92.234.400/0001-80;Outro;11.111.111/0001-91;Outro;11.111.111/0001-91;EST1;Duplicata;900,00;22/11/2026;22/10/2026\n");
            var a = new Ambiente(Pasta);
            Config = new ConfiguracaoService(a, Validadores);
            Estoque = new EstoqueService(a);
            Motor = new MotorRegras(Config, Estoque, new PrecificacaoService());
        }
        public void Dispose() => Directory.Delete(Pasta, true);
    }

    [Fact]
    public async Task LoteDeterministicoRegraDesativadaEDuplicidade()
    {
        using var c = new Cenario("{\"codigo\":\"CONCENTRACAO_MAXIMA_SACADO\",\"ativo\":true,\"parametros\":{\"percentual_maximo\":15,\"agrupamento\":\"RAIZ_CNPJ\"}}," +
            "{\"codigo\":\"PRAZO_MINIMO\",\"ativo\":false,\"parametros\":{\"prazo_minimo_dias\":90}}");
        var a = Nota("A", 100, 30);
        var b = Nota("B", 100, 30);
        // Carteira projetada: 900 do estoque + 200 do lote; o sacado soma 200 (18,2%) e todas as suas notas passam do limite.
        var primeiro = await c.Motor.Processar([b, a], false, default);
        var segundo = await c.Motor.Processar([a, b], false, default);
        Assert.All(primeiro.Resultados.Concat(segundo.Resultados), x => Assert.Equal(StatusRecebivel.RECUSADA, x.Status));
        Assert.All(primeiro.Resultados, x => Assert.Equal("CONCENTRACAO_MAXIMA_SACADO", Assert.Single(x.Motivos).Codigo));
        // Parcelas: mesmo documento com vencimentos diferentes não são duplicadas.
        var parcelas = await c.Motor.Processar([a, Nota("A", 10, 60)], false, default);
        Assert.Equal(2, parcelas.QuantidadeAprovada);
        Assert.Equal(98m + 9.6m, parcelas.ValorTotalLiquido);
        var duplicado = await c.Motor.Processar([a, a], false, default);
        Assert.Equal(2, duplicado.QuantidadeRecusada);
        Assert.Equal(0m, duplicado.ValorTotalLiquido);
        Assert.Equal(1, (await c.Motor.Processar([a], false, default)).QuantidadeAprovada);
        Assert.Equal(1, (await c.Motor.Processar([a], true, default)).QuantidadeAprovada);
        Assert.Equal(1, (await c.Motor.Processar([a], true, default)).QuantidadeRecusada);
    }

    [Fact]
    public async Task RecarregamentoEConfiguracaoInvalida()
    {
        using var c = new Cenario("{\"codigo\":\"VALOR_MINIMO\",\"ativo\":false,\"parametros\":{\"valor_minimo\":200}}");
        Assert.Equal(1, (await c.Motor.Processar([Nota()], false, default)).QuantidadeAprovada);
        var arquivo = Path.Combine(c.Dados, "validators.json");
        File.WriteAllText(arquivo, File.ReadAllText(arquivo).Replace("\"ativo\":false", "\"ativo\":true"));
        Assert.Equal(1, (await c.Motor.Processar([Nota()], false, default)).QuantidadeRecusada);
        File.WriteAllText(arquivo, File.ReadAllText(arquivo).Replace("VALOR_MINIMO", "DESCONHECIDA"));
        await Assert.ThrowsAsync<ConfiguracaoException>(() => c.Motor.Processar([Nota()], false, default));
        File.WriteAllText(arquivo, File.ReadAllText(arquivo).Replace("DESCONHECIDA", "VALOR_MINIMO").Replace("\"valor_minimo\":200", "\"valor_minimo\":-1"));
        Assert.Throws<ConfiguracaoException>(() => c.Config.Carregar());
    }
}
