using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class PagoVentaRepository(ProyectoCafeDbContext db) : IPagoVentaRepository
{
    public Task AddAsync(PagoVenta pago, CancellationToken ct)
    {
        db.PagosVenta.Add(pago);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PagoVenta>> ListByVentaAsync(int ventaId, CancellationToken ct) =>
        await db.PagosVenta
            .AsNoTracking()
            .Where(pago => pago.VentaId == ventaId)
            .OrderBy(pago => pago.FechaPago)
            .ThenBy(pago => pago.PagoVentaId)
            .ToListAsync(ct);
}