using antecipe_kanastra.Domain.Abstract;
using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Service;

public class Validator
{
    private readonly ValidadorContexto _contexto;

    public Validator(ValidadorContexto contexto)
    {
        _contexto = contexto;
    }

    public bool IsValid(List<Parametros> parametros)
    {
        return _contexto.Validar(parametros);
    }
}