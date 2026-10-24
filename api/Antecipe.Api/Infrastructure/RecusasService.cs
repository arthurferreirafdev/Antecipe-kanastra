using Antecipe.Api.Domain;

namespace Antecipe.Api.Infrastructure;

// Guarda em memória as notas recusadas de cada lote analisado; os lotes mais antigos saem ao atingir o limite.
public sealed class RecusasService
{
    private const int LimiteLotes = 200;
    private readonly Dictionary<Guid, IReadOnlyList<NotaRecusada>> _lotes = new();
    private readonly Queue<Guid> _ordem = new();
    private readonly Lock _trava = new();

    public Guid Registrar(string nomeArquivo, IEnumerable<ResultadoRecebivel> recusadas)
    {
        var loteId = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;
        var notas = recusadas.Select(r => new NotaRecusada(loteId, nomeArquivo, agora, r.Linha,
            r.NumeroDocumento, r.ValorNominal, r.Dados, r.Motivos)).ToList();
        lock (_trava)
        {
            _lotes[loteId] = notas;
            _ordem.Enqueue(loteId);
            while (_ordem.Count > LimiteLotes) _lotes.Remove(_ordem.Dequeue());
        }
        return loteId;
    }

    public IReadOnlyList<NotaRecusada>? Obter(Guid loteId)
    {
        lock (_trava) return _lotes.GetValueOrDefault(loteId);
    }
}
