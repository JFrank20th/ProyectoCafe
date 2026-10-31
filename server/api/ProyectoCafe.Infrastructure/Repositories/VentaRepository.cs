using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class VentaRepository(ProyectoCafeDbContext db) : IVentaRepository
{
    public async Task<IReadOnlyList<Venta>> ListAsync(CancellationToken ct)
    {
        return await db.Set<Venta>()
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PresentacionCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.TipoCafe)
            .OrderByDescending(x => x.FechaVenta)
            .ToListAsync(ct);
    }

    public Task<Venta?> FindAsync(int ventaId, CancellationToken ct)
    {
        return db.Set<Venta>()
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PresentacionCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.TipoCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PrecioVenta)
            .FirstOrDefaultAsync(x => x.VentaId == ventaId, ct);
    }

    public async Task<int> GetNextNumeroAsync(CancellationToken ct)
    {
        var last = await db.Set<Venta>()
            .OrderByDescending(x => x.NumeroVenta)
            .Select(x => (int?)x.NumeroVenta)
            .FirstOrDefaultAsync(ct);

        return (last ?? 0) + 1;
    }

    public Task AddAsync(Venta venta, CancellationToken ct)
    {
        db.Set<Venta>().Add(venta);
        return Task.CompletedTask;
    }
}