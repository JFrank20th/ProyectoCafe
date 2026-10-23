using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Application.Negocio.Ventas;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Inventario.Lotes;

namespace ProyectoCafe.Infrastructure.Negocio.Ventas;

public sealed class VentaRepository(ProyectoCafeDbContext dbContext) : IVentaRepository
{
    public async Task<IReadOnlyList<VentaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var ventas = await dbContext.Ventas
            .AsNoTracking()
            .OrderByDescending(x => x.VentaId)
            .Select(x => new VentaResponse
            {
                VentaId = x.VentaId,
                ClienteId = x.ClienteId,
                ClienteNombre = dbContext.Clientes
                    .Where(c => c.ClienteId == x.ClienteId)
                    .Select(c => c.Nombre)
                    .FirstOrDefault() ?? string.Empty,
                Fecha = x.Fecha,
                Total = x.Total,
                EstadoPago = x.EstadoPago,
                MetodoPagoId = dbContext.VentaPagos
                    .Where(vp => vp.VentaId == x.VentaId)
                    .Select(vp => (int?)vp.MetodoPagoId)
                    .FirstOrDefault(),
                MetodoPagoNombre = (
                    from vp in dbContext.VentaPagos
                    join mp in dbContext.MetodosPago on vp.MetodoPagoId equals mp.MetodoPagoId
                    where vp.VentaId == x.VentaId
                    select mp.Nombre
                ).FirstOrDefault(),
                CostoTotal = 0m,
                MargenBruto = 0m,
                RentabilidadPorcentaje = 0m,
                TipoPrecio = x.TipoPrecio,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);

        await AttachResumenCostosAsync(ventas, cancellationToken);
        return ventas;
    }

    public async Task<VentaViewResponse?> GetViewAsync(int ventaId, CancellationToken cancellationToken)
    {
        var venta = await dbContext.Ventas
            .AsNoTracking()
            .Where(x => x.VentaId == ventaId)
            .Select(x => new VentaResponse
            {
                VentaId = x.VentaId,
                ClienteId = x.ClienteId,
                ClienteNombre = dbContext.Clientes
                    .Where(c => c.ClienteId == x.ClienteId)
                    .Select(c => c.Nombre)
                    .FirstOrDefault() ?? string.Empty,
                Fecha = x.Fecha,
                Total = x.Total,
                EstadoPago = x.EstadoPago,
                MetodoPagoId = dbContext.VentaPagos
                    .Where(vp => vp.VentaId == x.VentaId)
                    .Select(vp => (int?)vp.MetodoPagoId)
                    .FirstOrDefault(),
                MetodoPagoNombre = (
                    from vp in dbContext.VentaPagos
                    join mp in dbContext.MetodosPago on vp.MetodoPagoId equals mp.MetodoPagoId
                    where vp.VentaId == x.VentaId
                    select mp.Nombre
                ).FirstOrDefault(),
                CostoTotal = 0m,
                MargenBruto = 0m,
                RentabilidadPorcentaje = 0m,
                TipoPrecio = x.TipoPrecio,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (venta is null)
        {
            return null;
        }

        var detalles = await (
            from detalle in dbContext.VentaDetalles.AsNoTracking()
            join producto in dbContext.Productos.AsNoTracking() on detalle.ProductoId equals producto.ProductoId
            join loteRef in dbContext.Lotes.AsNoTracking() on detalle.LoteId equals loteRef.LoteId into lotes
            from lote in lotes.DefaultIfEmpty()
            where detalle.VentaId == ventaId
            orderby detalle.DetalleId
            select new VentaDetalleResponse
            {
                DetalleId = detalle.DetalleId,
                VentaId = detalle.VentaId,
                ProductoId = detalle.ProductoId,
                ProductoNombre = producto.Nombre,
                PresentacionId = null,
                PresentacionCodigo = producto.PresentacionCodigo,
                PresentacionNombre = producto.PresentacionNombre,
                PresentacionCantidad = producto.PresentacionCantidad,
                PresentacionUnidadMedida = producto.PresentacionUnidadMedida,
                UnidadMedida = producto.UnidadMedida,
                LoteId = detalle.LoteId,
                CodigoLote = lote != null ? lote.CodigoLote : null,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                CostoUnitario = detalle.CostoUnitario,
                CostoTotal = 0m,
                MargenUnitario = 0m,
                MargenBruto = 0m,
                RentabilidadPorcentaje = 0m,
                Subtotal = detalle.Subtotal,
            })
            .ToListAsync(cancellationToken);

        foreach (var detalle in detalles)
        {
            detalle.CostoTotal = Math.Round(detalle.Cantidad * detalle.CostoUnitario, 2, MidpointRounding.AwayFromZero);
            detalle.MargenUnitario = Math.Round(detalle.PrecioUnitario - detalle.CostoUnitario, 4, MidpointRounding.AwayFromZero);
            detalle.MargenBruto = Math.Round(detalle.Subtotal - detalle.CostoTotal, 2, MidpointRounding.AwayFromZero);
            detalle.RentabilidadPorcentaje = detalle.Subtotal > 0
                ? Math.Round((detalle.MargenBruto / detalle.Subtotal) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
        }

        venta.CostoTotal = Math.Round(detalles.Sum(x => x.CostoTotal), 2, MidpointRounding.AwayFromZero);
        venta.MargenBruto = Math.Round(venta.Total - venta.CostoTotal, 2, MidpointRounding.AwayFromZero);
        venta.RentabilidadPorcentaje = venta.Total > 0
            ? Math.Round((venta.MargenBruto / venta.Total) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var pagos = await (
            from pago in dbContext.VentaPagos.AsNoTracking()
            join metodo in dbContext.MetodosPago.AsNoTracking() on pago.MetodoPagoId equals metodo.MetodoPagoId
            where pago.VentaId == ventaId
            orderby pago.VentaPagoId
            select new VentaPagoResponse
            {
                VentaPagoId = pago.VentaPagoId,
                VentaId = pago.VentaId,
                MetodoPagoId = pago.MetodoPagoId,
                MetodoPagoNombre = metodo.Nombre,
                Monto = pago.Monto,
            })
            .ToListAsync(cancellationToken);

        var loteMovimientos = await (
            from movimiento in dbContext.LoteMovimientos.AsNoTracking()
            join lote in dbContext.Lotes.AsNoTracking() on movimiento.LoteId equals lote.LoteId
            join producto in dbContext.Productos.AsNoTracking() on lote.ProductoId equals producto.ProductoId
            where movimiento.VentaId == ventaId
            orderby movimiento.Fecha, movimiento.MovimientoId
            select new VentaLoteMovimientoResponse
            {
                MovimientoId = movimiento.MovimientoId,
                LoteId = lote.LoteId,
                ProductoId = producto.ProductoId,
                ProductoNombre = producto.Nombre,
                UnidadMedida = producto.UnidadMedida,
                CodigoLote = lote.CodigoLote,
                Tipo = movimiento.Tipo,
                Fecha = movimiento.Fecha,
                Cantidad = movimiento.Cantidad,
                Observacion = movimiento.Observacion,
            })
            .ToListAsync(cancellationToken);

        return new VentaViewResponse
        {
            Venta = venta,
            Detalles = detalles,
            Pagos = pagos,
            LoteMovimientos = loteMovimientos,
        };
    }

    public async Task<int> CreateAsync(VentaCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.ClienteId <= 0)
        {
            throw new ArgumentException("ClienteId inválido.", nameof(request));
        }

        if (request.MetodoPagoId.HasValue && request.MetodoPagoId.Value <= 0)
        {
            throw new ArgumentException("MetodoPagoId inválido.", nameof(request));
        }

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("Detalles son requeridos.", nameof(request));
        }

        var estadoPago = NormalizeOptional(request.EstadoPago) ?? "Pagado";
        var tipoPrecio = NormalizeOptional(request.TipoPrecio) ?? "Normal";

        if (estadoPago.Length > 20)
        {
            throw new ArgumentException("EstadoPago no puede exceder 20 caracteres.", nameof(request));
        }

        if (tipoPrecio.Length > 30)
        {
            throw new ArgumentException("TipoPrecio no puede exceder 30 caracteres.", nameof(request));
        }

        if (!await dbContext.Clientes.AnyAsync(x => x.ClienteId == request.ClienteId, cancellationToken))
        {
            throw new ArgumentException("ClienteId inválido.", nameof(request));
        }

        var productoIds = request.Detalles
            .Select(x => x.ProductoId)
            .Distinct()
            .ToList();

        if (productoIds.Any(x => x <= 0))
        {
            throw new ArgumentException("ProductoId inválido.", nameof(request));
        }

        var productos = await dbContext.Productos
            .Where(x => productoIds.Contains(x.ProductoId))
            .ToListAsync(cancellationToken);

        if (productos.Count != productoIds.Count)
        {
            throw new ArgumentException("Uno o más ProductoId no existen.", nameof(request));
        }

        var productosById = productos.ToDictionary(x => x.ProductoId);
        var tipoVentaId = await ResolveTipoVentaIdAsync(tipoPrecio, cancellationToken);
        var preciosPorProducto = await LoadPreciosDinamicosAsync(productoIds, tipoVentaId, cancellationToken);
        var loteCostCalculator = new LoteCostCalculator(dbContext);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        decimal total = 0m;

        foreach (var detalle in request.Detalles)
        {
            if (detalle.Cantidad <= 0)
            {
                throw new ArgumentException("Cantidad debe ser mayor a 0.", nameof(request));
            }

            if (detalle.LoteId.HasValue && detalle.LoteId.Value <= 0)
            {
                throw new ArgumentException("LoteId inválido.", nameof(request));
            }

            var producto = productosById[detalle.ProductoId];

            if (!producto.PermiteDecimal && detalle.Cantidad != decimal.Truncate(detalle.Cantidad))
            {
                throw new ArgumentException($"ProductoId {detalle.ProductoId} no permite cantidad decimal.", nameof(request));
            }

            var precioUnitario = detalle.PrecioUnitario ?? ResolvePrecio(producto, tipoPrecio, preciosPorProducto, detalle.ProductoId);
            if (precioUnitario < 0)
            {
                throw new ArgumentException("PrecioUnitario no puede ser negativo.", nameof(request));
            }

            var stock = producto.Stock ?? 0m;
            if (stock < detalle.Cantidad)
            {
                throw new ArgumentException($"Stock insuficiente para ProductoId {detalle.ProductoId}.", nameof(request));
            }

            total += Math.Round(detalle.Cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);
        }

        var venta = new Venta
        {
            ClienteId = request.ClienteId,
            Total = total,
            EstadoPago = estadoPago,
            TipoPrecio = tipoPrecio,
            Estado = null,
            Fecha = null,
        };

        dbContext.Ventas.Add(venta);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.MetodoPagoId.HasValue)
        {
            var existsMetodo = await dbContext.MetodosPago
                .AnyAsync(x => x.MetodoPagoId == request.MetodoPagoId.Value, cancellationToken);

            if (!existsMetodo)
            {
                throw new ArgumentException("MetodoPagoId inválido.", nameof(request));
            }

            dbContext.VentaPagos.Add(new VentaPago
            {
                VentaId = venta.VentaId,
                MetodoPagoId = request.MetodoPagoId.Value,
                Monto = total,
            });
        }

        foreach (var detalle in request.Detalles)
        {
            var producto = productosById[detalle.ProductoId];
            var precioUnitario = detalle.PrecioUnitario ?? ResolvePrecio(producto, tipoPrecio, preciosPorProducto, detalle.ProductoId);
            var costoTotalDetalle = detalle.LoteId.HasValue
                ? await ApplySalidaLoteAsync(venta.VentaId, detalle.ProductoId, detalle.LoteId.Value, detalle.Cantidad, loteCostCalculator, cancellationToken)
                : await ApplySalidaFifoAsync(venta.VentaId, detalle.ProductoId, detalle.Cantidad, loteCostCalculator, cancellationToken);
            var costoUnitario = detalle.Cantidad > 0
                ? Math.Round(costoTotalDetalle / detalle.Cantidad, 4, MidpointRounding.AwayFromZero)
                : 0m;

            dbContext.VentaDetalles.Add(new VentaDetalle
            {
                VentaId = venta.VentaId,
                ProductoId = detalle.ProductoId,
                LoteId = detalle.LoteId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = precioUnitario,
                CostoUnitario = costoUnitario,
            });

            producto.Stock = (producto.Stock ?? 0m) - detalle.Cantidad;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return venta.VentaId;
    }

    public async Task<bool> AnnulAsync(int ventaId, CancellationToken cancellationToken)
    {
        var venta = await dbContext.Ventas
            .SingleOrDefaultAsync(x => x.VentaId == ventaId, cancellationToken);

        if (venta is null)
        {
            return false;
        }

        if (!(venta.Estado ?? true))
        {
            throw new ArgumentException("La venta ya está anulada.");
        }

        var detalles = await dbContext.VentaDetalles
            .Where(x => x.VentaId == ventaId)
            .ToListAsync(cancellationToken);

        if (detalles.Count == 0)
        {
            throw new ArgumentException("La venta no tiene detalles para revertir.");
        }

        var movimientosSalida = await dbContext.LoteMovimientos
            .Where(x => x.VentaId == ventaId && x.Tipo == "Salida")
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.MovimientoId)
            .ToListAsync(cancellationToken);

        var productoIds = detalles.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await dbContext.Productos
            .Where(x => productoIds.Contains(x.ProductoId))
            .ToDictionaryAsync(x => x.ProductoId, cancellationToken);

        var loteIds = movimientosSalida.Select(x => x.LoteId).Distinct().ToList();
        var lotes = loteIds.Count == 0
            ? new Dictionary<int, Lote>()
            : await dbContext.Lotes
                .Where(x => loteIds.Contains(x.LoteId))
                .ToDictionaryAsync(x => x.LoteId, cancellationToken);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var detalle in detalles)
        {
            if (!productos.TryGetValue(detalle.ProductoId, out var producto))
            {
                throw new ArgumentException($"ProductoId {detalle.ProductoId} no existe para revertir la venta.");
            }

            producto.Stock = (producto.Stock ?? 0m) + detalle.Cantidad;
        }

        foreach (var movimiento in movimientosSalida)
        {
            if (!lotes.TryGetValue(movimiento.LoteId, out var lote))
            {
                throw new ArgumentException($"LoteId {movimiento.LoteId} no existe para revertir la venta.");
            }

            lote.CantidadDisponible += movimiento.Cantidad;

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = movimiento.LoteId,
                Tipo = "Entrada",
                VentaId = ventaId,
                Cantidad = movimiento.Cantidad,
                Observacion = "Anulación de venta",
            });
        }

        venta.Estado = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<decimal> ApplySalidaLoteAsync(
        int ventaId,
        int productoId,
        int loteId,
        decimal cantidad,
        LoteCostCalculator loteCostCalculator,
        CancellationToken cancellationToken)
    {
        var lote = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == loteId, cancellationToken);

        if (lote is null)
        {
            throw new ArgumentException("LoteId inválido.");
        }

        if (lote.ProductoId != productoId)
        {
            throw new ArgumentException("LoteId no corresponde al producto.");
        }

        if (lote.CantidadDisponible < cantidad)
        {
            throw new ArgumentException("Stock insuficiente en el lote.");
        }

        var costo = await loteCostCalculator.CalculateAsync(loteId, cancellationToken);

        lote.CantidadDisponible -= cantidad;

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = loteId,
            Tipo = "Salida",
            VentaId = ventaId,
            Cantidad = cantidad,
            Observacion = "Venta",
        });

        return Math.Round(costo.CostoUnitario * cantidad, 4, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> ApplySalidaFifoAsync(
        int ventaId,
        int productoId,
        decimal cantidad,
        LoteCostCalculator loteCostCalculator,
        CancellationToken cancellationToken)
    {
        var remaining = cantidad;
        var costoTotal = 0m;

        var lotes = await dbContext.Lotes
            .Where(x => x.ProductoId == productoId && x.Estado == "Activo" && x.CantidadDisponible > 0)
            .OrderBy(x => x.FechaCreacion)
            .ThenBy(x => x.LoteId)
            .ToListAsync(cancellationToken);

        foreach (var lote in lotes)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(remaining, lote.CantidadDisponible);
            if (take <= 0)
            {
                continue;
            }

            lote.CantidadDisponible -= take;
            remaining -= take;

            var costo = await loteCostCalculator.CalculateAsync(lote.LoteId, cancellationToken);
            costoTotal += costo.CostoUnitario * take;

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = lote.LoteId,
                Tipo = "Salida",
                VentaId = ventaId,
                Cantidad = take,
                Observacion = "Venta (FIFO)",
            });
        }

        if (remaining > 0)
        {
            throw new ArgumentException($"Stock insuficiente en lotes para ProductoId {productoId}.");
        }

        return Math.Round(costoTotal, 4, MidpointRounding.AwayFromZero);
    }

    private async Task AttachResumenCostosAsync(IReadOnlyList<VentaResponse> ventas, CancellationToken cancellationToken)
    {
        if (ventas.Count == 0)
        {
            return;
        }

        var ventaIds = ventas.Select(x => x.VentaId).ToList();
        var costos = await dbContext.VentaDetalles
            .AsNoTracking()
            .Where(x => ventaIds.Contains(x.VentaId))
            .GroupBy(x => x.VentaId)
            .Select(x => new
            {
                VentaId = x.Key,
                CostoTotal = x.Sum(y => y.Cantidad * y.CostoUnitario),
            })
            .ToDictionaryAsync(x => x.VentaId, x => x.CostoTotal, cancellationToken);

        foreach (var venta in ventas)
        {
            var costoTotal = costos.TryGetValue(venta.VentaId, out var costo) ? costo : 0m;
            venta.CostoTotal = Math.Round(costoTotal, 2, MidpointRounding.AwayFromZero);
            venta.MargenBruto = Math.Round(venta.Total - venta.CostoTotal, 2, MidpointRounding.AwayFromZero);
            venta.RentabilidadPorcentaje = venta.Total > 0
                ? Math.Round((venta.MargenBruto / venta.Total) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
        }
    }

    private async Task<int?> ResolveTipoVentaIdAsync(string tipoPrecio, CancellationToken cancellationToken)
    {
        var normalizedTipoPrecio = NormalizeOptional(tipoPrecio);
        if (normalizedTipoPrecio is null)
        {
            return null;
        }

        return await dbContext.TiposVenta
            .AsNoTracking()
            .Where(x => (x.Estado ?? true) && (x.Codigo == normalizedTipoPrecio || x.Nombre == normalizedTipoPrecio))
            .Select(x => (int?)x.TipoVentaId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<int, decimal>> LoadPreciosDinamicosAsync(IReadOnlyList<int> productoIds, int? tipoVentaId, CancellationToken cancellationToken)
    {
        if (!tipoVentaId.HasValue || productoIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        return await dbContext.ProductosPreciosTipoVenta
            .AsNoTracking()
            .Where(x => x.TipoVentaId == tipoVentaId.Value && productoIds.Contains(x.ProductoId) && x.Estado != false)
            .ToDictionaryAsync(x => x.ProductoId, x => x.Precio, cancellationToken);
    }

    private static decimal ResolvePrecio(Producto producto, string tipoPrecio, IReadOnlyDictionary<int, decimal> preciosPorProducto, int productoId)
    {
        if (preciosPorProducto.TryGetValue(productoId, out var precioDinamico))
        {
            return precioDinamico;
        }

        if (tipoPrecio.Equals(TiposVentaPredefinidos.Internet, StringComparison.OrdinalIgnoreCase))
        {
            return producto.PrecioInternet;
        }

        if (tipoPrecio.Equals(TiposVentaPredefinidos.Mayorista, StringComparison.OrdinalIgnoreCase))
        {
            return producto.PrecioMayorista;
        }

        return producto.Precio;
    }

    private static string? NormalizeOptional(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
}
