using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.Insumos;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Insumos;

public sealed class InsumoRepository(ProyectoCafeDbContext dbContext) : IInsumoRepository
{
    public async Task<IReadOnlyList<InsumoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Insumos
            .AsNoTracking()
            .OrderBy(x => x.InsumoId)
            .Select(x => new InsumoResponse
            {
                InsumoId = x.InsumoId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Cantidad = x.Cantidad ?? 0m,
                CostoUnitario = x.CostoUnitario ?? 0m,
                UnidadMedida = x.UnidadMedida,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<InsumoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Insumos
            .AsNoTracking()
            .Where(x => x.InsumoId == id)
            .Select(x => new InsumoResponse
            {
                InsumoId = x.InsumoId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Cantidad = x.Cantidad ?? 0m,
                CostoUnitario = x.CostoUnitario ?? 0m,
                UnidadMedida = x.UnidadMedida,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(InsumoRequest request, CancellationToken cancellationToken)
    {
        var nombre = NormalizeNombre(request.Nombre);
        if (nombre is null)
        {
            throw new ArgumentException("Nombre es requerido.", nameof(request));
        }

        var unidad = NormalizeUnidadMedida(request.UnidadMedida) ?? "UN";

        var entity = new Insumo
        {
            Nombre = nombre,
            Descripcion = NormalizeDescripcion(request.Descripcion),
            Cantidad = request.Cantidad,
            CostoUnitario = request.CostoUnitario,
            UnidadMedida = unidad,
            Estado = request.Estado,
        };

        dbContext.Insumos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.InsumoId;
    }

    public async Task<bool> UpdateAsync(int id, InsumoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Insumos
            .SingleOrDefaultAsync(x => x.InsumoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var nombre = NormalizeNombre(request.Nombre);
        if (nombre is not null)
        {
            entity.Nombre = nombre;
        }

        entity.Descripcion = NormalizeDescripcion(request.Descripcion);
        entity.Cantidad = request.Cantidad;
    entity.CostoUnitario = request.CostoUnitario;
        entity.UnidadMedida = NormalizeUnidadMedida(request.UnidadMedida) ?? entity.UnidadMedida;
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Insumos
            .SingleOrDefaultAsync(x => x.InsumoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Insumos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NormalizeNombre(string? nombre)
    {
        var value = (nombre ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeDescripcion(string? descripcion)
    {
        var value = (descripcion ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeUnidadMedida(string? unidadMedida)
    {
        var value = (unidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
