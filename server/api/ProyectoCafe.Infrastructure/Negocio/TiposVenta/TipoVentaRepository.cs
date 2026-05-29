using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Negocio.TiposVenta;

public sealed class TipoVentaRepository(ProyectoCafeDbContext dbContext) : ITipoVentaRepository
{
    public async Task<IReadOnlyList<TipoVentaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.TiposVenta
            .AsNoTracking()
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.TipoVentaId)
            .Select(x => new TipoVentaResponse
            {
                TipoVentaId = x.TipoVentaId,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Orden = x.Orden,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TipoVentaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.TiposVenta
            .AsNoTracking()
            .Where(x => x.TipoVentaId == id)
            .Select(x => new TipoVentaResponse
            {
                TipoVentaId = x.TipoVentaId,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Orden = x.Orden,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(TipoVentaRequest request, CancellationToken cancellationToken)
    {
        var orden = request.Orden ?? await GetNextOrdenAsync(cancellationToken);

        var entity = new TipoVenta
        {
            Codigo = NormalizeCodigo(request.Codigo),
            Nombre = NormalizeNombre(request.Nombre),
            Orden = orden,
            Estado = request.Estado,
        };

        dbContext.TiposVenta.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.TipoVentaId;
    }

    public async Task<bool> UpdateAsync(int id, TipoVentaRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TiposVenta
            .SingleOrDefaultAsync(x => x.TipoVentaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Codigo = NormalizeCodigo(request.Codigo);
        entity.Nombre = NormalizeNombre(request.Nombre);
        if (request.Orden.HasValue)
        {
            entity.Orden = request.Orden.Value;
        }

        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TiposVenta
            .SingleOrDefaultAsync(x => x.TipoVentaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.TiposVenta.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
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

    private async Task<int> GetNextOrdenAsync(CancellationToken cancellationToken)
    {
        var maxOrden = await dbContext.TiposVenta
            .AsNoTracking()
            .Select(x => (int?)x.Orden)
            .MaxAsync(cancellationToken);

        return (maxOrden ?? 0) + 10;
    }
}
