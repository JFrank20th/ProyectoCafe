using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Lotes;

internal readonly record struct LoteCostSnapshot(decimal CostoTotal, decimal CostoUnitario);

internal sealed class LoteCostCalculator(ProyectoCafeDbContext dbContext)
{
    private readonly Dictionary<int, LoteCostSnapshot> cache = [];

    public Task<LoteCostSnapshot> CalculateAsync(int loteId, CancellationToken cancellationToken)
        => CalculateAsync(loteId, new HashSet<int>(), cancellationToken);

    private async Task<LoteCostSnapshot> CalculateAsync(int loteId, HashSet<int> stack, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(loteId, out var snapshot))
        {
            return snapshot;
        }

        if (!stack.Add(loteId))
        {
            throw new InvalidOperationException($"Se detectó una referencia circular calculando costo para el lote {loteId}.");
        }

        try
        {
            var lote = await dbContext.Lotes
                .AsNoTracking()
                .Where(x => x.LoteId == loteId)
                .Select(x => new { x.CantidadInicial })
                .SingleOrDefaultAsync(cancellationToken);

            if (lote is null)
            {
                throw new ArgumentException($"LoteId {loteId} inválido.", nameof(loteId));
            }

            var costoDirecto = await dbContext.Costos
                .AsNoTracking()
                .Where(x => x.LoteId == loteId && x.Estado != false)
                .SumAsync(x => (decimal?)x.Monto, cancellationToken) ?? 0m;

            decimal costoOrigen = 0m;

            var proceso = await dbContext.ProcesosInventario
                .AsNoTracking()
                .Where(x => x.LoteSalidaId == loteId && x.Estado != "Anulado")
                .Select(x => new { x.LoteOrigenId, x.CantidadOrigenConsumida })
                .SingleOrDefaultAsync(cancellationToken);

            if (proceso?.LoteOrigenId is int loteOrigenId)
            {
                var origen = await CalculateAsync(loteOrigenId, stack, cancellationToken);
                costoOrigen = Math.Round(origen.CostoUnitario * proceso.CantidadOrigenConsumida, 4, MidpointRounding.AwayFromZero);
            }
            else
            {
                var recepcionTotal = await dbContext.RecepcionesCafe
                    .AsNoTracking()
                    .Where(x => x.LoteId == loteId && x.Estado != "Anulada")
                    .Select(x => (decimal?)x.Total)
                    .SingleOrDefaultAsync(cancellationToken);

                if (recepcionTotal.HasValue)
                {
                    costoOrigen = recepcionTotal.Value;
                }
            }

            var costoTotal = costoDirecto + costoOrigen;
            var costoUnitario = lote.CantidadInicial > 0
                ? Math.Round(costoTotal / lote.CantidadInicial, 6, MidpointRounding.AwayFromZero)
                : 0m;

            snapshot = new LoteCostSnapshot(costoTotal, costoUnitario);
            cache[loteId] = snapshot;
            return snapshot;
        }
        finally
        {
            stack.Remove(loteId);
        }
    }
}
