using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.Productos;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Productos;

public sealed class ProductoRepository(ProyectoCafeDbContext dbContext) : IProductoRepository
{
    public async Task<IReadOnlyList<ProductoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var productos = await dbContext.Productos
            .AsNoTracking()
            .OrderBy(producto => producto.ProductoId)
            .Select(producto => new ProductoResponse
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                PrecioInternet = producto.PrecioInternet,
                PrecioMayorista = producto.PrecioMayorista,
                Stock = producto.Stock ?? 0m,
                ManejaLotes = producto.ManejaLotes,
                TipoLote = producto.TipoLote,
                Estado = producto.Estado ?? true,
                PresentacionCodigo = producto.PresentacionCodigo,
                PresentacionNombre = producto.PresentacionNombre,
                PresentacionCantidad = producto.PresentacionCantidad,
                PresentacionUnidadMedida = producto.PresentacionUnidadMedida,
                UnidadMedida = producto.UnidadMedida,
                PermiteDecimal = producto.PermiteDecimal,
            })
            .ToListAsync(cancellationToken);

        await AttachPreciosTipoVentaAsync(productos, cancellationToken);
        return productos;
    }

    public async Task<ProductoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var producto = await dbContext.Productos
            .AsNoTracking()
            .Where(entity => entity.ProductoId == id)
            .Select(entity => new ProductoResponse
            {
                ProductoId = entity.ProductoId,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Precio = entity.Precio,
                PrecioInternet = entity.PrecioInternet,
                PrecioMayorista = entity.PrecioMayorista,
                Stock = entity.Stock ?? 0m,
                ManejaLotes = entity.ManejaLotes,
                TipoLote = entity.TipoLote,
                Estado = entity.Estado ?? true,
                PresentacionCodigo = entity.PresentacionCodigo,
                PresentacionNombre = entity.PresentacionNombre,
                PresentacionCantidad = entity.PresentacionCantidad,
                PresentacionUnidadMedida = entity.PresentacionUnidadMedida,
                UnidadMedida = entity.UnidadMedida,
                PermiteDecimal = entity.PermiteDecimal,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (producto is null)
        {
            return null;
        }

        await AttachPreciosTipoVentaAsync([producto], cancellationToken);
        return producto;
    }

    public async Task<int> CreateAsync(ProductoRequest request, CancellationToken cancellationToken)
    {
        var normalizedNombre = NormalizeNombre(request.Nombre);
        if (normalizedNombre is null)
        {
            throw new ArgumentException("Nombre es requerido.", nameof(request));
        }

        var unidadMedida = NormalizeUnidadMedida(request.UnidadMedida);
        if (unidadMedida is null)
        {
            throw new ArgumentException("La unidad de medida es requerida.", nameof(request));
        }

        await EnsureUnidadMedidaExistsAsync(unidadMedida, nameof(request.UnidadMedida), cancellationToken);

        var entity = new Producto
        {
            Nombre = normalizedNombre,
            Descripcion = NormalizeDescripcion(request.Descripcion),
            Precio = request.Precio,
            PrecioInternet = request.PrecioInternet,
            PrecioMayorista = request.PrecioMayorista,
            Stock = request.Stock,
            ManejaLotes = true,
            TipoLote = "Manual",
            Estado = request.Estado,
            PresentacionCodigo = null,
            PresentacionNombre = null,
            PresentacionCantidad = null,
            PresentacionUnidadMedida = null,
            UnidadMedida = unidadMedida,
            PermiteDecimal = true,
        };

        await SyncLegacyPricesFromDynamicAsync(entity, request, cancellationToken);

        dbContext.Productos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncPreciosTipoVentaAsync(entity.ProductoId, request, cancellationToken);
        return entity.ProductoId;
    }

    public async Task<bool> UpdateAsync(int id, ProductoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var unidadMedida = NormalizeUnidadMedida(request.UnidadMedida);
        if (unidadMedida is null)
        {
            throw new ArgumentException("La unidad de medida es requerida.", nameof(request));
        }

        await EnsureUnidadMedidaExistsAsync(unidadMedida, nameof(request.UnidadMedida), cancellationToken);

        var normalizedNombre = NormalizeNombre(request.Nombre);
        if (normalizedNombre is not null)
        {
            entity.Nombre = normalizedNombre;
        }

        entity.Descripcion = NormalizeDescripcion(request.Descripcion);
        entity.Precio = request.Precio;
        entity.PrecioInternet = request.PrecioInternet;
        entity.PrecioMayorista = request.PrecioMayorista;
        entity.Stock = request.Stock;
        entity.ManejaLotes = true;
        entity.TipoLote = "Manual";
        entity.Estado = request.Estado;
        entity.PresentacionCodigo = null;
        entity.PresentacionNombre = null;
        entity.PresentacionCantidad = null;
        entity.PresentacionUnidadMedida = null;
        entity.UnidadMedida = unidadMedida;
        entity.PermiteDecimal = true;

        await SyncLegacyPricesFromDynamicAsync(entity, request, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncPreciosTipoVentaAsync(entity.ProductoId, request, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        await EnsureProductCanBeDeletedAsync(id, cancellationToken);

        var precios = await dbContext.ProductosPreciosTipoVenta
            .Where(x => x.ProductoId == id)
            .ToListAsync(cancellationToken);

        if (precios.Count > 0)
        {
            dbContext.ProductosPreciosTipoVenta.RemoveRange(precios);
        }

        dbContext.Productos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureProductCanBeDeletedAsync(int productoId, CancellationToken cancellationToken)
    {
        if (await dbContext.Lotes.AnyAsync(x => x.ProductoId == productoId, cancellationToken)
            || await dbContext.Cultivos.AnyAsync(x => x.ProductoCosechaId == productoId, cancellationToken)
            || await dbContext.OperacionesAgricolas.AnyAsync(x => x.ProductoId == productoId, cancellationToken)
            || await dbContext.CosechasCafe.AnyAsync(x => x.ProductoId == productoId, cancellationToken)
            || await dbContext.ProcesosInventario.AnyAsync(x => x.ProductoSalidaId == productoId, cancellationToken)
            || await dbContext.VentaDetalles.AnyAsync(x => x.ProductoId == productoId, cancellationToken))
        {
            throw new ArgumentException("No se puede eliminar el producto porque ya tiene uso operacional. Inactivalo en su lugar.", nameof(productoId));
        }
    }

    private async Task AttachPreciosTipoVentaAsync(IReadOnlyList<ProductoResponse> productos, CancellationToken cancellationToken)
    {
        if (productos.Count == 0)
        {
            return;
        }

        var productoIds = productos.Select(x => x.ProductoId).ToList();

        var precios = await (
            from precio in dbContext.ProductosPreciosTipoVenta.AsNoTracking()
            join tipoVenta in dbContext.TiposVenta.AsNoTracking() on precio.TipoVentaId equals tipoVenta.TipoVentaId
            where productoIds.Contains(precio.ProductoId)
            orderby precio.ProductoId, tipoVenta.Orden, tipoVenta.TipoVentaId
            select new
            {
                precio.ProductoId,
                Response = new ProductoPrecioTipoVentaResponse
                {
                    TipoVentaId = precio.TipoVentaId,
                    TipoVentaCodigo = tipoVenta.Codigo,
                    TipoVentaNombre = tipoVenta.Nombre,
                    Precio = precio.Precio,
                    Estado = precio.Estado ?? true,
                },
            })
            .ToListAsync(cancellationToken);

        var preciosPorProducto = precios
            .GroupBy(x => x.ProductoId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<ProductoPrecioTipoVentaResponse>)x.Select(y => y.Response).ToList());

        foreach (var producto in productos)
        {
            producto.PreciosTipoVenta = preciosPorProducto.TryGetValue(producto.ProductoId, out var list)
                ? list
                : [];
        }
    }

    private async Task EnsureUnidadMedidaExistsAsync(string? unidadMedida, string parameterName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(unidadMedida))
        {
            return;
        }

        var exists = await dbContext.UnidadesMedida
            .AnyAsync(x => x.Codigo == unidadMedida && x.Estado != false, cancellationToken);

        if (!exists)
        {
            throw new ArgumentException("La unidad de medida seleccionada es inválida o está inactiva.", parameterName);
        }
    }

    private async Task SyncLegacyPricesFromDynamicAsync(Producto entity, ProductoRequest request, CancellationToken cancellationToken)
    {
        if (request.PreciosTipoVenta is not { Count: > 0 })
        {
            return;
        }

        var tipoVentaIds = request.PreciosTipoVenta
            .Select(x => x.TipoVentaId)
            .Distinct()
            .ToList();

        var tiposVenta = await dbContext.TiposVenta
            .AsNoTracking()
            .Where(x => tipoVentaIds.Contains(x.TipoVentaId))
            .Select(x => new { x.TipoVentaId, x.Codigo })
            .ToListAsync(cancellationToken);

        foreach (var precio in request.PreciosTipoVenta)
        {
            var codigo = tiposVenta
                .Where(x => x.TipoVentaId == precio.TipoVentaId)
                .Select(x => x.Codigo)
                .SingleOrDefault();

            if (string.Equals(codigo, TiposVentaPredefinidos.Normal, StringComparison.OrdinalIgnoreCase))
            {
                entity.Precio = precio.Precio;
            }
            else if (string.Equals(codigo, TiposVentaPredefinidos.Internet, StringComparison.OrdinalIgnoreCase))
            {
                entity.PrecioInternet = precio.Precio;
            }
            else if (string.Equals(codigo, TiposVentaPredefinidos.Mayorista, StringComparison.OrdinalIgnoreCase))
            {
                entity.PrecioMayorista = precio.Precio;
            }
        }
    }

    private async Task SyncPreciosTipoVentaAsync(int productoId, ProductoRequest request, CancellationToken cancellationToken)
    {
        List<ProductoPrecioTipoVentaRequest> preciosDeseados;

        if (request.PreciosTipoVenta is { Count: > 0 })
        {
            var duplicateTipoVentaId = request.PreciosTipoVenta
                .GroupBy(x => x.TipoVentaId)
                .Where(x => x.Key > 0 && x.Count() > 1)
                .Select(x => x.Key)
                .FirstOrDefault();

            if (duplicateTipoVentaId > 0)
            {
                throw new ArgumentException($"TipoVentaId {duplicateTipoVentaId} está repetido.", nameof(request));
            }

            preciosDeseados = request.PreciosTipoVenta.ToList();
        }
        else
        {
            var tiposBase = await dbContext.TiposVenta
                .AsNoTracking()
                .Where(x => TiposVentaPredefinidos.CodigosBase.Contains(x.Codigo))
                .Select(x => new { x.TipoVentaId, x.Codigo })
                .ToListAsync(cancellationToken);

            preciosDeseados = tiposBase
                .Select(x => new ProductoPrecioTipoVentaRequest
                {
                    TipoVentaId = x.TipoVentaId,
                    Precio = ResolveLegacyPrice(request, x.Codigo),
                    Estado = true,
                })
                .ToList();
        }

        var tipoVentaIds = preciosDeseados
            .Select(x => x.TipoVentaId)
            .Distinct()
            .ToList();

        if (tipoVentaIds.Count == 0)
        {
            return;
        }

        if (tipoVentaIds.Any(x => x <= 0))
        {
            throw new ArgumentException("TipoVentaId inválido en PreciosTipoVenta.", nameof(request));
        }

        if (preciosDeseados.Any(x => x.Precio < 0))
        {
            throw new ArgumentException("Precio no puede ser negativo en PreciosTipoVenta.", nameof(request));
        }

        var tiposVentaExistentes = await dbContext.TiposVenta
            .Where(x => tipoVentaIds.Contains(x.TipoVentaId))
            .Select(x => x.TipoVentaId)
            .ToListAsync(cancellationToken);

        if (tiposVentaExistentes.Count != tipoVentaIds.Count)
        {
            throw new ArgumentException("Uno o más TipoVentaId no existen.", nameof(request));
        }

        var preciosActuales = await dbContext.ProductosPreciosTipoVenta
            .Where(x => x.ProductoId == productoId && tipoVentaIds.Contains(x.TipoVentaId))
            .ToListAsync(cancellationToken);

        foreach (var precioDeseado in preciosDeseados)
        {
            var actual = preciosActuales.SingleOrDefault(x => x.TipoVentaId == precioDeseado.TipoVentaId);
            if (actual is null)
            {
                dbContext.ProductosPreciosTipoVenta.Add(new ProductoPrecioTipoVenta
                {
                    ProductoId = productoId,
                    TipoVentaId = precioDeseado.TipoVentaId,
                    Precio = precioDeseado.Precio,
                    Estado = precioDeseado.Estado,
                });
                continue;
            }

            actual.Precio = precioDeseado.Precio;
            actual.Estado = precioDeseado.Estado;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static string? NormalizeTipoLote(string? tipoLote)
    {
        var value = (tipoLote ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeUnidadMedida(string? unidadMedida)
    {
        var value = (unidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizePresentacionCodigo(string? presentacionCodigo)
    {
        var value = (presentacionCodigo ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal ResolveLegacyPrice(ProductoRequest request, string tipoVentaCodigo)
    {
        if (tipoVentaCodigo.Equals(TiposVentaPredefinidos.Internet, StringComparison.OrdinalIgnoreCase))
        {
            return request.PrecioInternet;
        }

        if (tipoVentaCodigo.Equals(TiposVentaPredefinidos.Mayorista, StringComparison.OrdinalIgnoreCase))
        {
            return request.PrecioMayorista;
        }

        return request.Precio;
    }
}
