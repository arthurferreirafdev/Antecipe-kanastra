using antecipe_kanastra.Domain.Enums;

namespace antecipe_kanastra.Domain.Aggregates;

public record Parametros(ValidatorsEnum Codigo, string Value);