using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Gastos;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Contabilidad.Gastos;

public sealed class GastoRepository(ProyectoCafeDbContext dbContext) : IGastoRepository
{
    public async Task<IReadOnlyList<GastoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Gastos
            .AsNoTracking()
            .OrderBy(x => x.GastoId)
            .Select(x => new GastoResponse
            {
                GastoId = x.GastoId,
                Nombre = x.Nombre,
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GastoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Gastos
            .AsNoTracking()
            .Where(x => x.GastoId == id)
            .Select(x => new GastoResponse
            {
                GastoId = x.GastoId,
                Nombre = x.Nombre,
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(GastoRequest request, CancellationToken cancellationToken)
    {
        var nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");

        var entity = new Gasto
        {
            Nombre = nombre,
            Monto = request.Monto,
            Estado = request.Estado,
        };

        if (request.Fecha.HasValue)
        {
            entity.Fecha = request.Fecha.Value;
        }

        dbContext.Gastos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.GastoId;
    }

    public async Task<bool> UpdateAsync(int id, GastoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Gastos
            .SingleOrDefaultAsync(x => x.GastoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");
        entity.Monto = request.Monto;
        entity.Estado = request.Estado;
        if (request.Fecha.HasValue)
        {
            entity.Fecha = request.Fecha.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Gastos
            .SingleOrDefaultAsync(x => x.GastoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Gastos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        var v = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(v))
        {
            throw new ArgumentException(errorMessage);
        }

        return v;
    }
}
