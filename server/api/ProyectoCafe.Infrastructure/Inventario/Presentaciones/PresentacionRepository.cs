using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.Presentaciones;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Presentaciones;

public sealed class PresentacionRepository(ProyectoCafeDbContext dbContext) : IPresentacionRepository
{
    public async Task<IReadOnlyList<PresentacionResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Presentaciones
            .AsNoTracking()
            .OrderBy(x => x.UnidadMedida)
            .ThenBy(x => x.Cantidad)
            .ThenBy(x => x.PresentacionId)
            .Select(x => new PresentacionResponse
            {
                PresentacionId = x.PresentacionId,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Cantidad = x.Cantidad,
                UnidadMedida = x.UnidadMedida,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PresentacionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Presentaciones
            .AsNoTracking()
            .Where(x => x.PresentacionId == id)
            .Select(x => new PresentacionResponse
            {
                PresentacionId = x.PresentacionId,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Cantidad = x.Cantidad,
                UnidadMedida = x.UnidadMedida,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(PresentacionRequest request, CancellationToken cancellationToken)
    {
        var unidadMedida = NormalizeUnidadMedida(request.UnidadMedida);
        await EnsureUnidadMedidaExistsAsync(unidadMedida, cancellationToken);

        var entity = new Presentacion
        {
            Codigo = NormalizeCodigo(request.Codigo),
            Nombre = NormalizeNombre(request.Nombre),
            Cantidad = request.Cantidad,
            PesoGramosLegacy = request.Cantidad,
            UnidadMedida = unidadMedida,
            Estado = request.Estado,
        };

        dbContext.Presentaciones.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.PresentacionId;
    }

    public async Task<bool> UpdateAsync(int id, PresentacionRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Presentaciones
            .SingleOrDefaultAsync(x => x.PresentacionId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var unidadMedida = NormalizeUnidadMedida(request.UnidadMedida);
        await EnsureUnidadMedidaExistsAsync(unidadMedida, cancellationToken);

        entity.Codigo = NormalizeCodigo(request.Codigo);
        entity.Nombre = NormalizeNombre(request.Nombre);
        entity.Cantidad = request.Cantidad;
        entity.PesoGramosLegacy = request.Cantidad;
        entity.UnidadMedida = unidadMedida;
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Presentaciones
            .SingleOrDefaultAsync(x => x.PresentacionId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Presentaciones.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUnidadMedidaExistsAsync(string unidadMedida, CancellationToken cancellationToken)
    {
        var exists = await dbContext.UnidadesMedida
            .AnyAsync(x => x.Codigo == unidadMedida && x.Estado != false, cancellationToken);

        if (!exists)
        {
            throw new ArgumentException("UnidadMedida inválida o inactiva.", nameof(unidadMedida));
        }
    }

    private static string NormalizeCodigo(string? codigo)
    {
        var value = (codigo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Codigo es requerido.", nameof(codigo));
        }

        return value;
    }

    private static string NormalizeNombre(string? nombre)
    {
        var value = (nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Nombre es requerido.", nameof(nombre));
        }

        return value;
    }

    private static string NormalizeUnidadMedida(string? unidadMedida)
    {
        var value = (unidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("UnidadMedida es requerida.", nameof(unidadMedida));
        }

        return value;
    }
}