using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.DTOs.Ventas;
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
            .Include(x => x.Pagos)
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
            .Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.VentaId == ventaId, ct);
    }

    public Task<Venta?> FindWithPagosAsync(int ventaId, CancellationToken ct)
    {
        return db.Set<Venta>()
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PresentacionCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.TipoCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PrecioVenta)
            .Include(x => x.Pagos)
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

    public async Task<IReadOnlyList<Venta>> ListFilteredAsync(VentaFiltro filtro, CancellationToken ct)
    {
        IQueryable<Venta> query = db.Set<Venta>()
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.PresentacionCafe)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.TipoCafe)
            .Include(x => x.Pagos);

        if (filtro.NumeroVenta.HasValue)
        {
            query = query.Where(x => x.NumeroVenta == filtro.NumeroVenta.Value);
        }

        if (filtro.ClienteId.HasValue)
        {
            query = query.Where(x => x.ClienteId == filtro.ClienteId.Value);
        }

        if (filtro.Estado.HasValue)
        {
            query = query.Where(x => (int)x.Estado == filtro.Estado.Value);
        }

        if (filtro.EstadoEntrega.HasValue)
        {
            query = query.Where(x => (int)x.EstadoEntrega == filtro.EstadoEntrega.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            query = query.Where(x => x.FechaVenta >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            var hasta = filtro.FechaHasta.Value.Date.AddDays(1);
            query = query.Where(x => x.FechaVenta < hasta);
        }

        if (filtro.EstadoPago.HasValue)
        {
            if (filtro.EstadoPago.Value == (int)EstadoPagoVenta.PendientePago)
            {
                query = query.Where(x => !x.Pagos.Any());
            }
            else if (filtro.EstadoPago.Value == (int)EstadoPagoVenta.AbonoParcial)
            {
                query = query.Where(x => x.Pagos.Any() && x.Pagos.Sum(p => p.Monto) < x.Total);
            }
            else if (filtro.EstadoPago.Value == (int)EstadoPagoVenta.Pagada)
            {
                query = query.Where(x => x.Pagos.Sum(p => p.Monto) >= x.Total);
            }
        }

        return await query
            .OrderByDescending(x => x.FechaVenta)
            .ToListAsync(ct);
    }

    public Task AddAsync(Venta venta, CancellationToken ct)
    {
        db.Set<Venta>().Add(venta);
        return Task.CompletedTask;
    }
}