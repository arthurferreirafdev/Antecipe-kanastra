using Antecipe.Api.Domain;

namespace Antecipe.Api.Application;

public sealed class PrecificacaoService
{
    public decimal Calcular(Recebivel r, DateOnly dataBase, PrecificacaoConfig config)
    {
        var dias = r.DataVencimento.DayNumber - dataBase.DayNumber;
        var desconto = r.ValorNominal * (config.TaxaMensalPercentual / 100m) * dias / config.BaseDias;
        var liquido = Math.Round(r.ValorNominal - desconto, 2, MidpointRounding.AwayFromZero);
        if (liquido < 0) throw new InvalidOperationException("Valor líquido negativo. Operação não pode ser precificada.");
        return liquido;
    }
}
