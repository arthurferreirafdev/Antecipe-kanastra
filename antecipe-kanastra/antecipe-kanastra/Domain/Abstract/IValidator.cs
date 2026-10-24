namespace antecipe_kanastra.Domain.Abstract;

public interface IValidator
{
    bool IsValid(string Codigo, Dictionary<string, List<string>> Parametros, bool Ativo);
}