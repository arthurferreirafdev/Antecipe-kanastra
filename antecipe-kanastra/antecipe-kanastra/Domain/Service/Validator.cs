using antecipe_kanastra.Domain.Abstract;
using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Service;

public class Validator : IValidator
{
    public bool IsValid(string Codigo, List<Parametros> Parametros, bool Ativo)
    {
        return false;
    }
}