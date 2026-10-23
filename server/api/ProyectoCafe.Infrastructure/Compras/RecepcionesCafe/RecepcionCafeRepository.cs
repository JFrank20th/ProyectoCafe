using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Compras.RecepcionesCafe;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Compras.RecepcionesCafe;

public sealed class RecepcionCafeRepository(ProyectoCafeDbContext dbContext) : IRecepcionCafeRepository
{
    public async Task<IReadOnlyList<RecepcionCafeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RecepcionesCafe
            .AsNoTracking()
            .Join(
                dbContext.Proveedores.AsNoTracking(),
                recepcion => recepcion.ProveedorId,
                proveedor => proveedor.ProveedorId,
                (recepcion, proveedor) => new { recepcion, proveedor })
            .Join(
                dbContext.Productos.AsNoTracking(),
                x => x.recepcion.ProductoId,
                producto => producto.ProductoId,
                (x, producto) => new { x.recepcion, x.proveedor, producto })
            .OrderByDescending(x => x.recepcion.FechaRecepcion)
            .ThenByDescending(x => x.recepcion.RecepcionCafeId)
            .Select(x => new RecepcionCafeResponse
            {
                RecepcionCafeId = x.recepcion.RecepcionCafeId,
                ProveedorId = x.recepcion.ProveedorId,
                ProveedorNombre = x.proveedor.Nombre,
                ProductoId = x.recepcion.ProductoId,
                ProductoNombre = x.producto.Nombre,
                UnidadMedida = "KG",
                TipoCafe = x.recepcion.TipoCafe,
                FechaRecepcion = x.recepcion.FechaRecepcion,
                CodigoLote = x.recepcion.CodigoLote,
                Cantidad = x.recepcion.Cantidad,
                CantidadDisponible = x.recepcion.CantidadDisponible,
                PrecioUnitario = x.recepcion.PrecioUnitario,
                Total = x.recepcion.Total,
                Humedad = x.recepcion.Humedad,
                Observaciones = x.recepcion.Observaciones,
                Estado = x.recepcion.Estado,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RecepcionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.RecepcionesCafe
            .AsNoTracking()
            .Join(
                dbContext.Proveedores.AsNoTracking(),
                recepcion => recepcion.ProveedorId,
                proveedor => proveedor.ProveedorId,
                (recepcion, proveedor) => new { recepcion, proveedor })
            .Join(
                dbContext.Productos.AsNoTracking(),
                x => x.recepcion.ProductoId,
                producto => producto.ProductoId,
                (x, producto) => new { x.recepcion, x.proveedor, producto })
            .Where(x => x.recepcion.RecepcionCafeId == id)
            .Select(x => new RecepcionCafeResponse
            {
                RecepcionCafeId = x.recepcion.RecepcionCafeId,
                ProveedorId = x.recepcion.ProveedorId,
                ProveedorNombre = x.proveedor.Nombre,
                ProductoId = x.recepcion.ProductoId,
                ProductoNombre = x.producto.Nombre,
                UnidadMedida = "KG",
                TipoCafe = x.recepcion.TipoCafe,
                FechaRecepcion = x.recepcion.FechaRecepcion,
                CodigoLote = x.recepcion.CodigoLote,
                Cantidad = x.recepcion.Cantidad,
                CantidadDisponible = x.recepcion.CantidadDisponible,
                PrecioUnitario = x.recepcion.PrecioUnitario,
                Total = x.recepcion.Total,
                Humedad = x.recepcion.Humedad,
                Observaciones = x.recepcion.Observaciones,
                Estado = x.recepcion.Estado,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RecepcionCafeCreateResult> CreateAsync(RecepcionCafeCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.ProveedorId <= 0)
        {
            throw new ArgumentException("ProveedorId inválido.", nameof(request));
        }

        if (request.ProductoId <= 0)
        {
            throw new ArgumentException("ProductoId inválido.", nameof(request));
        }

        var tipoCafe = TiposCafeCompra.Normalize(request.TipoCafe);
        if (tipoCafe is null)
        {
            throw new ArgumentException("TipoCafe inválido.", nameof(request));
        }

        if (request.Cantidad <= 0)
        {
            throw new ArgumentException("Cantidad debe ser mayor a 0.", nameof(request));
        }

        if (request.PrecioUnitario < 0)
        {
            throw new ArgumentException("PrecioUnitario no puede ser negativo.", nameof(request));
        }

        if (request.Humedad.HasValue && (request.Humedad.Value < 0 || request.Humedad.Value > 100))
        {
            throw new ArgumentException("Humedad debe estar entre 0 y 100.", nameof(request));
        }

        var proveedor = await dbContext.Proveedores
            .SingleOrDefaultAsync(x => x.ProveedorId == request.ProveedorId, cancellationToken);

        if (proveedor is null || proveedor.Estado == false)
        {
            throw new ArgumentException("ProveedorId inválido o inactivo.", nameof(request));
        }

        var producto = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == request.ProductoId, cancellationToken);

        if (producto is null || producto.Estado == false)
        {
            throw new ArgumentException("ProductoId inválido o inactivo.", nameof(request));
        }

        if (!producto.PermiteDecimal && request.Cantidad != decimal.Truncate(request.Cantidad))
        {
            throw new ArgumentException($"ProductoId {request.ProductoId} no permite cantidad decimal.", nameof(request));
        }

        var fechaBase = (request.FechaRecepcion ?? DateTime.Today).Date;
        var codigoLote = await AutoGenerarCodigoLoteCompraAsync(tipoCafe, request.ProductoId, fechaBase, cancellationToken);
        var total = Math.Round(request.Cantidad * request.PrecioUnitario, 2, MidpointRounding.AwayFromZero);

        try
        {
            var recepcion = new RecepcionCafe
            {
                ProveedorId = request.ProveedorId,
                ProductoId = request.ProductoId,
                TipoCafe = tipoCafe,
                FechaRecepcion = request.FechaRecepcion,
                CodigoLote = codigoLote,
                Cantidad = request.Cantidad,
                CantidadDisponible = request.Cantidad,
                PrecioUnitario = request.PrecioUnitario,
                Total = total,
                Humedad = request.Humedad,
                Observaciones = NormalizeOptional(request.Observaciones),
                Estado = "Registrada",
                LoteId = null,
            };

            dbContext.RecepcionesCafe.Add(recepcion);

            await dbContext.SaveChangesAsync(cancellationToken);

            return new RecepcionCafeCreateResult
            {
                RecepcionCafeId = recepcion.RecepcionCafeId,
                CodigoLote = recepcion.CodigoLote,
            };
        }
        catch (DbUpdateException ex)
        {
            throw new ArgumentException("No se pudo registrar la recepción. Verifica que el código de lote no esté duplicado.", nameof(request), ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var recepcion = await dbContext.RecepcionesCafe
            .SingleOrDefaultAsync(x => x.RecepcionCafeId == id, cancellationToken);

        if (recepcion is null)
        {
            return false;
        }

        if (recepcion.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (recepcion.CantidadDisponible != recepcion.Cantidad)
        {
            throw new ArgumentException("No se puede anular porque la recepción ya tuvo procesos posteriores.", nameof(id));
        }

        recepcion.Estado = "Anulada";
        recepcion.CantidadDisponible = 0m;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> AutoGenerarCodigoLoteCompraAsync(string tipoCafe, int productoId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"CMP-{MapTipoCafePrefix(tipoCafe)}-{productoId:000}-{fecha:yyyyMMdd}-";

        var correlativo = await dbContext.RecepcionesCafe
            .AsNoTracking()
            .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);

        return $"{prefix}{correlativo + 1:000}";
    }

    private static string MapTipoCafePrefix(string tipoCafe)
    {
        if (tipoCafe.Equals(TiposCafeCompra.Cereza, StringComparison.OrdinalIgnoreCase))
        {
            return "CER";
        }

        if (tipoCafe.Equals(TiposCafeCompra.Oreado, StringComparison.OrdinalIgnoreCase))
        {
            return "ORE";
        }

        if (tipoCafe.Equals(TiposCafeCompra.RecienLavado, StringComparison.OrdinalIgnoreCase))
        {
            return "LAV";
        }

        return "SEC";
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
