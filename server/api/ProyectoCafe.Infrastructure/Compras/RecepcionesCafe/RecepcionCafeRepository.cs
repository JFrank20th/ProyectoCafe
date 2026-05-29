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
            .Join(
                dbContext.Lotes.AsNoTracking(),
                x => x.recepcion.LoteId,
                lote => lote.LoteId,
                (x, lote) => new { x.recepcion, x.proveedor, x.producto, lote })
            .OrderByDescending(x => x.recepcion.FechaRecepcion)
            .ThenByDescending(x => x.recepcion.RecepcionCafeId)
            .Select(x => new RecepcionCafeResponse
            {
                RecepcionCafeId = x.recepcion.RecepcionCafeId,
                ProveedorId = x.recepcion.ProveedorId,
                ProveedorNombre = x.proveedor.Nombre,
                ProductoId = x.recepcion.ProductoId,
                ProductoNombre = x.producto.Nombre,
                TipoCafe = x.recepcion.TipoCafe,
                FechaRecepcion = x.recepcion.FechaRecepcion,
                NumeroDocumento = x.recepcion.NumeroDocumento,
                Cantidad = x.recepcion.Cantidad,
                PrecioUnitario = x.recepcion.PrecioUnitario,
                Total = x.recepcion.Total,
                Humedad = x.recepcion.Humedad,
                Observaciones = x.recepcion.Observaciones,
                Estado = x.recepcion.Estado,
                LoteId = x.recepcion.LoteId,
                CodigoLote = x.lote.CodigoLote,
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
            .Join(
                dbContext.Lotes.AsNoTracking(),
                x => x.recepcion.LoteId,
                lote => lote.LoteId,
                (x, lote) => new { x.recepcion, x.proveedor, x.producto, lote })
            .Where(x => x.recepcion.RecepcionCafeId == id)
            .Select(x => new RecepcionCafeResponse
            {
                RecepcionCafeId = x.recepcion.RecepcionCafeId,
                ProveedorId = x.recepcion.ProveedorId,
                ProveedorNombre = x.proveedor.Nombre,
                ProductoId = x.recepcion.ProductoId,
                ProductoNombre = x.producto.Nombre,
                TipoCafe = x.recepcion.TipoCafe,
                FechaRecepcion = x.recepcion.FechaRecepcion,
                NumeroDocumento = x.recepcion.NumeroDocumento,
                Cantidad = x.recepcion.Cantidad,
                PrecioUnitario = x.recepcion.PrecioUnitario,
                Total = x.recepcion.Total,
                Humedad = x.recepcion.Humedad,
                Observaciones = x.recepcion.Observaciones,
                Estado = x.recepcion.Estado,
                LoteId = x.recepcion.LoteId,
                CodigoLote = x.lote.CodigoLote,
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
        var codigoLote = NormalizeCodigoLote(request.CodigoLote) ?? await AutoGenerarCodigoLoteCompraAsync(tipoCafe, request.ProductoId, fechaBase, cancellationToken);
        var total = Math.Round(request.Cantidad * request.PrecioUnitario, 2, MidpointRounding.AwayFromZero);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var lote = new Lote
            {
                ProductoId = request.ProductoId,
                CodigoLote = codigoLote,
                CantidadInicial = request.Cantidad,
                CantidadDisponible = request.Cantidad,
                Estado = "Activo",
            };

            dbContext.Lotes.Add(lote);
            producto.ManejaLotes = true;
            producto.Stock = (producto.Stock ?? 0m) + request.Cantidad;

            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = lote.LoteId,
                Tipo = "IngresoCompra",
                VentaId = null,
                Cantidad = request.Cantidad,
                Observacion = $"Ingreso por compra a proveedor #{proveedor.ProveedorId}",
            });

            var recepcion = new RecepcionCafe
            {
                ProveedorId = request.ProveedorId,
                ProductoId = request.ProductoId,
                TipoCafe = tipoCafe,
                FechaRecepcion = request.FechaRecepcion,
                NumeroDocumento = NormalizeOptional(request.NumeroDocumento),
                Cantidad = request.Cantidad,
                PrecioUnitario = request.PrecioUnitario,
                Total = total,
                Humedad = request.Humedad,
                Observaciones = NormalizeOptional(request.Observaciones),
                Estado = "Registrada",
                LoteId = lote.LoteId,
            };

            dbContext.RecepcionesCafe.Add(recepcion);

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new RecepcionCafeCreateResult
            {
                RecepcionCafeId = recepcion.RecepcionCafeId,
                LoteId = lote.LoteId,
                CodigoLote = lote.CodigoLote,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar la recepción. Verifica que el código de lote y el documento del proveedor no estén duplicados.", nameof(request), ex);
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

        var lote = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == recepcion.LoteId, cancellationToken);

        if (lote is null)
        {
            throw new ArgumentException("El lote asociado no existe.", nameof(id));
        }

        if (lote.CantidadDisponible != recepcion.Cantidad || lote.CantidadInicial != recepcion.Cantidad)
        {
            throw new ArgumentException("No se puede anular porque el lote ya tuvo movimientos posteriores.", nameof(id));
        }

        var producto = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == recepcion.ProductoId, cancellationToken);

        if (producto is null)
        {
            throw new ArgumentException("El producto asociado no existe.", nameof(id));
        }

        if ((producto.Stock ?? 0m) < recepcion.Cantidad)
        {
            throw new ArgumentException("No se puede anular porque el stock actual es menor al recibido.", nameof(id));
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        producto.Stock = (producto.Stock ?? 0m) - recepcion.Cantidad;
        lote.CantidadDisponible = 0m;
        lote.Estado = "Inactivo";
        recepcion.Estado = "Anulada";

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = lote.LoteId,
            Tipo = "AnulacionCompra",
            VentaId = null,
            Cantidad = recepcion.Cantidad,
            Observacion = $"Anulacion de compra #{recepcion.RecepcionCafeId}",
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<string> AutoGenerarCodigoLoteCompraAsync(string tipoCafe, int productoId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"CMP-{MapTipoCafePrefix(tipoCafe)}-{productoId:000}-{fecha:yyyyMMdd}-";

        var correlativo = await dbContext.Lotes
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

        if (tipoCafe.Equals(TiposCafeCompra.RecienLavado, StringComparison.OrdinalIgnoreCase))
        {
            return "LAV";
        }

        return "SEC";
    }

    private static string? NormalizeCodigoLote(string? codigoLote)
    {
        var normalized = (codigoLote ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
