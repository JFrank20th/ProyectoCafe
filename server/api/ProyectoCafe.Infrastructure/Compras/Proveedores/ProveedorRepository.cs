using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Compras.Proveedores;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Compras.Proveedores;

public sealed class ProveedorRepository(ProyectoCafeDbContext dbContext) : IProveedorRepository
{
    public async Task<IReadOnlyList<ProveedorResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Proveedores
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.ProveedorId)
            .Select(x => new ProveedorResponse
            {
                ProveedorId = x.ProveedorId,
                Nombre = x.Nombre,
                Documento = x.Documento,
                Telefono = x.Telefono,
                Email = x.Email,
                Direccion = x.Direccion,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProveedorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Proveedores
            .AsNoTracking()
            .Where(x => x.ProveedorId == id)
            .Select(x => new ProveedorResponse
            {
                ProveedorId = x.ProveedorId,
                Nombre = x.Nombre,
                Documento = x.Documento,
                Telefono = x.Telefono,
                Email = x.Email,
                Direccion = x.Direccion,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(ProveedorRequest request, CancellationToken cancellationToken)
    {
        var entity = new Proveedor
        {
            Nombre = request.Nombre ?? string.Empty,
            Documento = NullIfWhiteSpace(request.Documento),
            Telefono = NullIfWhiteSpace(request.Telefono),
            Email = NormalizeEmail(request.Email),
            Direccion = NullIfWhiteSpace(request.Direccion),
            Estado = request.Estado,
        };

        dbContext.Proveedores.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ProveedorId;
    }

    public async Task<bool> UpdateAsync(int id, ProveedorRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Proveedores
            .SingleOrDefaultAsync(x => x.ProveedorId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Nombre = request.Nombre ?? entity.Nombre;
        entity.Documento = NullIfWhiteSpace(request.Documento);
        entity.Telefono = NullIfWhiteSpace(request.Telefono);
        entity.Email = NormalizeEmail(request.Email);
        entity.Direccion = NullIfWhiteSpace(request.Direccion);
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Proveedores
            .SingleOrDefaultAsync(x => x.ProveedorId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Proveedores.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToLowerInvariant();
    }
}
