using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Abstract;

public interface IValidator
{
    bool IsValid(string Codigo, List<Parametros> Parametros, bool Ativo);
}