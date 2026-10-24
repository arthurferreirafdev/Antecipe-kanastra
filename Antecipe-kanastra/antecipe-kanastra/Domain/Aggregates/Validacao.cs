using antecipe_kanastra.Domain.Aggregates;
using antecipe_kanastra.Domain.Services.Validadores;

namespace antecipe_kanastra.Domain.Aggregates;

public sealed class Validacao
{
    private readonly IReadOnlyList<IValidador> _validadores;

    public Validacao(IEnumerable<IValidador> validadores)
    {
        _validadores = validadores.Where(v => v.Ativo).ToList();
    }

    public bool Validar(List<Parametros> parametros)
    {
        foreach (var validador in _validadores)
        {
            if (!validador.Validar(parametros))
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<(string Codigo, bool Valido)> ValidarDetalhado(List<Parametros> parametros)
    {
        return _validadores
            .Select(v => (v.Codigo, v.Validar(parametros)))
            .ToList();
    }
}
