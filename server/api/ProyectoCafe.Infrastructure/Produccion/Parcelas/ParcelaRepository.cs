using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Produccion.Parcelas;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Produccion.Parcelas;

public sealed class ParcelaRepository(ProyectoCafeDbContext dbContext) : IParcelaRepository
{
    public async Task<IReadOnlyList<ParcelaResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken)
    {
        var query =
            from parcela in dbContext.Parcelas.AsNoTracking()
            join finca in dbContext.Fincas.AsNoTracking() on parcela.FincaId equals finca.FincaId
            join productoRef in dbContext.Productos.AsNoTracking() on parcela.ProductoCosechaId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            select new { parcela, finca, producto };

        if (fincaId.HasValue)
        {
            query = query.Where(x => x.parcela.FincaId == fincaId.Value);
        }

        return await query
            .OrderBy(x => x.finca.Nombre)
            .ThenBy(x => x.parcela.Nombre)
            .Select(x => new ParcelaResponse
            {
                ParcelaId = x.parcela.ParcelaId,
                FincaId = x.parcela.FincaId,
                ProductoCosechaId = x.parcela.ProductoCosechaId,
                ProductoCosechaNombre = x.producto != null ? x.producto.Nombre : null,
                FincaNombre = x.finca.Nombre,
                Nombre = x.parcela.Nombre,
                Descripcion = x.parcela.Descripcion,
                Cultivo = x.parcela.Cultivo,
                AreaHectareas = x.parcela.AreaHectareas,
                Estado = x.parcela.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ParcelaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from parcela in dbContext.Parcelas.AsNoTracking()
            join finca in dbContext.Fincas.AsNoTracking() on parcela.FincaId equals finca.FincaId
            join productoRef in dbContext.Productos.AsNoTracking() on parcela.ProductoCosechaId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            where parcela.ParcelaId == id
            select new ParcelaResponse
            {
                ParcelaId = parcela.ParcelaId,
                FincaId = parcela.FincaId,
                ProductoCosechaId = parcela.ProductoCosechaId,
                ProductoCosechaNombre = producto != null ? producto.Nombre : null,
                FincaNombre = finca.Nombre,
                Nombre = parcela.Nombre,
                Descripcion = parcela.Descripcion,
                Cultivo = parcela.Cultivo,
                AreaHectareas = parcela.AreaHectareas,
                Estado = parcela.Estado ?? true,
            }
        )
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(ParcelaRequest request, CancellationToken cancellationToken)
    {
        var existsFinca = await dbContext.Fincas
            .AnyAsync(x => x.FincaId == request.FincaId, cancellationToken);

        if (!existsFinca)
        {
            throw new ArgumentException("FincaId inválido.", nameof(request));
        }

        var productoCosechaId = await ValidateProductoCosechaAsync(request.ProductoCosechaId, cancellationToken);

        var entity = new Parcela
        {
            FincaId = request.FincaId,
            ProductoCosechaId = productoCosechaId,
            Nombre = request.Nombre ?? string.Empty,
            Descripcion = NullIfWhiteSpace(request.Descripcion),
            Cultivo = NullIfWhiteSpace(request.Cultivo),
            AreaHectareas = request.AreaHectareas,
            Estado = request.Estado,
        };

        dbContext.Parcelas.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ParcelaId;
    }

    public async Task<bool> UpdateAsync(int id, ParcelaRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Parcelas
            .SingleOrDefaultAsync(x => x.ParcelaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var existsFinca = await dbContext.Fincas
            .AnyAsync(x => x.FincaId == request.FincaId, cancellationToken);

        if (!existsFinca)
        {
            throw new ArgumentException("FincaId inválido.", nameof(request));
        }

        var productoCosechaId = await ValidateProductoCosechaAsync(request.ProductoCosechaId, cancellationToken);

        entity.FincaId = request.FincaId;
        entity.ProductoCosechaId = productoCosechaId;
        entity.Nombre = request.Nombre ?? entity.Nombre;
        entity.Descripcion = NullIfWhiteSpace(request.Descripcion);
        entity.Cultivo = NullIfWhiteSpace(request.Cultivo);
        entity.AreaHectareas = request.AreaHectareas;
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Parcelas
            .SingleOrDefaultAsync(x => x.ParcelaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Parcelas.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task<int> ValidateProductoCosechaAsync(int? productoCosechaId, CancellationToken cancellationToken)
    {
        if (productoCosechaId is not int productoId)
        {
            throw new ArgumentException("ProductoCosechaId es requerido.", nameof(productoCosechaId));
        }

        var existsProducto = await dbContext.Productos
            .AnyAsync(x => x.ProductoId == productoId && x.Estado != false, cancellationToken);

        if (!existsProducto)
        {
            throw new ArgumentException("ProductoCosechaId inválido o inactivo.", nameof(productoCosechaId));
        }

        return productoId;
    }
}
