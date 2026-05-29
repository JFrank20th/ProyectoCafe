using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Pasivos;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Contabilidad.Pasivos;

public sealed class PasivoRepository(ProyectoCafeDbContext dbContext) : IPasivoRepository
{
    public async Task<IReadOnlyList<PasivoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Pasivos
            .AsNoTracking()
            .OrderBy(x => x.PasivoId)
            .Select(x => new PasivoResponse
            {
                PasivoId = x.PasivoId,
                Nombre = x.Nombre,
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PasivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Pasivos
            .AsNoTracking()
            .Where(x => x.PasivoId == id)
            .Select(x => new PasivoResponse
            {
                PasivoId = x.PasivoId,
                Nombre = x.Nombre,
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(PasivoRequest request, CancellationToken cancellationToken)
    {
        var nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");

        var entity = new Pasivo
        {
            Nombre = nombre,
            Monto = request.Monto,
            Estado = request.Estado,
        };

        if (request.Fecha.HasValue)
        {
            entity.Fecha = request.Fecha.Value;
        }

        dbContext.Pasivos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.PasivoId;
    }

    public async Task<bool> UpdateAsync(int id, PasivoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Pasivos
            .SingleOrDefaultAsync(x => x.PasivoId == id, cancellationToken);

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
        var entity = await dbContext.Pasivos
            .SingleOrDefaultAsync(x => x.PasivoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Pasivos.Remove(entity);
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
