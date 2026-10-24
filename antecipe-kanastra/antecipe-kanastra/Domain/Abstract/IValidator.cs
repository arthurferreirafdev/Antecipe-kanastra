using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Abstract;

public interface IValidator
{
    string Codigo { get; }

    bool Ativo { get; }

    bool IsValid(List<Parametros> Parametros);
}
