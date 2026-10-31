using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class PrecioVentaRepository(ProyectoCafeDbContext db) : IPrecioVentaRepository
{
    public async Task<IReadOnlyList<PrecioVenta>> ListAsync(CancellationToken ct)
    {
        return await db.PreciosVenta
            .AsNoTracking()
            .Include(x => x.PresentacionCafe)
            .OrderBy(x => x.PresentacionCafe!.Nombre)
            .ThenBy(x => x.Nombre)
            .ToListAsync(ct);
    }

    public Task<PrecioVenta?> FindAsync(int precioVentaId, CancellationToken ct)
    {
        return db.PreciosVenta
            .Include(x => x.PresentacionCafe)
            .FirstOrDefaultAsync(x => x.PrecioVentaId == precioVentaId, ct);
    }

    public async Task<bool> NombreExistsAsync(
        string nombre,
        int presentacionCafeId,
        int? excludePrecioVentaId,
        CancellationToken ct)
    {
        return await db.PreciosVenta.AnyAsync(
            x => x.Nombre == nombre &&
                 x.PresentacionCafeId == presentacionCafeId &&
                 (!excludePrecioVentaId.HasValue || x.PrecioVentaId != excludePrecioVentaId.Value),
            ct);
    }

    public Task AddAsync(PrecioVenta precioVenta, CancellationToken ct)
    {
        db.PreciosVenta.Add(precioVenta);
        return Task.CompletedTask;
    }
}