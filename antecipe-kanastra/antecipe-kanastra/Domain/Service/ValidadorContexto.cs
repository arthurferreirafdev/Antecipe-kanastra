using antecipe_kanastra.Domain.Abstract;
using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Service;

public sealed class ValidadorContexto
{
    private readonly IReadOnlyList<IValidator> _validadores;

    public ValidadorContexto(IEnumerable<IValidator> validadores)
    {
        _validadores = validadores.Where(v => v.Ativo).ToList();
    }

    public bool Validar(List<Parametros> parametros)
    {
        foreach (var validador in _validadores)
        {
            if (!validador.IsValid(parametros))
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<(string Codigo, bool Valido)> ValidarDetalhado(List<Parametros> parametros)
    {
        return _validadores
            .Select(v => (v.Codigo, v.IsValid(parametros)))
            .ToList();
    }
}
