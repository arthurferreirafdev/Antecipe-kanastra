using antecipe_kanastra.Domain.Aggregates;

namespace antecipe_kanastra.Domain.Services.Validadores;

public interface IValidador
{
    string Codigo { get; }

    bool Ativo { get; }

    bool Validar(List<Parametros> parametros);
}
