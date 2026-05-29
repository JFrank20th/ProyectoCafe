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

            var costoEmpaqueOrigen = await dbContext.EmpaquesCafeDetalles
                .AsNoTracking()
                .Where(x => x.LoteSalidaId == loteId && x.Estado != "Anulada")
                .Select(x => (decimal?)x.CostoOrigenAsignado)
                .SingleOrDefaultAsync(cancellationToken);

            if (costoEmpaqueOrigen.HasValue)
            {
                costoOrigen = costoEmpaqueOrigen.Value;
            }
            else
            {
                var transformacion = await dbContext.TransformacionesCafe
                    .AsNoTracking()
                    .Where(x => x.LoteSalidaId == loteId && x.Estado != "Anulada")
                    .Select(x => new { x.LoteOrigenId, x.CantidadEntrada })
                    .SingleOrDefaultAsync(cancellationToken);

                if (transformacion is not null)
                {
                    var origen = await CalculateAsync(transformacion.LoteOrigenId, stack, cancellationToken);
                    costoOrigen = Math.Round(origen.CostoUnitario * transformacion.CantidadEntrada, 4, MidpointRounding.AwayFromZero);
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
