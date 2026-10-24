using antecipe_kanastra.Domain;

namespace antecipe_kanastra.Infrastructure;

public sealed class EstoqueService
{
    private sealed class Indice
    {
        public decimal Total;
        public readonly Dictionary<string, decimal> Sacados = new(StringComparer.Ordinal);
        public readonly Dictionary<string, decimal> Cedentes = new(StringComparer.Ordinal);
        public readonly HashSet<string> Chaves = new(StringComparer.Ordinal);
    }

    private readonly Dictionary<string, Indice> _fundos = new(StringComparer.Ordinal);
    public SemaphoreSlim Exclusao { get; } = new(1, 1);

    public EstoqueService(IWebHostEnvironment ambiente)
    {
        var arquivo = Environment.GetEnvironmentVariable("ANTECIPE_ESTOQUE")
            ?? Path.Combine(ambiente.ContentRootPath, "estoque_antecipe.csv");
        using var stream = File.OpenRead(arquivo);
        foreach (var r in CsvRecebiveis.Ler(stream)) Adicionar(r);
    }

    // Chamado apenas na inicialização ou sob Exclusao durante a confirmação.
    public bool Adicionar(Recebivel r)
    {
        var fundo = Documentos.Digitos(r.DocFundo);
        if (!_fundos.TryGetValue(fundo, out var indice)) _fundos[fundo] = indice = new Indice();
        if (!indice.Chaves.Add(Documentos.Chave(r))) return false;
        indice.Total += r.ValorNominal;
        var sacado = Documentos.Raiz(r.DocSacado);
        var cedente = Documentos.Digitos(r.DocCedente);
        indice.Sacados[sacado] = indice.Sacados.GetValueOrDefault(sacado) + r.ValorNominal;
        indice.Cedentes[cedente] = indice.Cedentes.GetValueOrDefault(cedente) + r.ValorNominal;
        return true;
    }

    // Chamado sob Exclusao para garantir uma visão consistente na simulação/aquisição.
    public CarteiraSnapshot Snapshot(string docFundo)
    {
        if (!_fundos.TryGetValue(Documentos.Digitos(docFundo), out var indice))
            return new CarteiraSnapshot(0, new Dictionary<string, decimal>(), new Dictionary<string, decimal>(), new HashSet<string>());
        return new CarteiraSnapshot(indice.Total,
            new Dictionary<string, decimal>(indice.Sacados, StringComparer.Ordinal),
            new Dictionary<string, decimal>(indice.Cedentes, StringComparer.Ordinal),
            new HashSet<string>(indice.Chaves, StringComparer.Ordinal));
    }
}
