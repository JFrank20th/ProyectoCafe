using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.Lotes;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Lotes;

public sealed class LoteRepository(ProyectoCafeDbContext dbContext) : ILoteRepository
{
    public async Task<IReadOnlyList<LoteExistenciaResumenResponse>> GetSummaryAsync(int? productoId, bool soloActivos, bool soloConStock, CancellationToken cancellationToken)
    {
        var query = dbContext.Lotes.AsNoTracking().AsQueryable();

        if (productoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == productoId.Value);
        }

        if (soloActivos)
        {
            query = query.Where(x => x.Estado == "Activo");
        }

        if (soloConStock)
        {
            query = query.Where(x => x.CantidadDisponible > 0);
        }

        return await query
            .Join(
                dbContext.Productos.AsNoTracking(),
                lote => lote.ProductoId,
                producto => producto.ProductoId,
                (lote, producto) => new { lote, producto })
            .GroupBy(x => new { x.lote.ProductoId, x.producto.Nombre, x.producto.UnidadMedida })
            .Select(group => new LoteExistenciaResumenResponse
            {
                ProductoId = group.Key.ProductoId,
                ProductoNombre = group.Key.Nombre,
                UnidadMedida = group.Key.UnidadMedida,
                Lotes = group.Count(),
                LotesActivos = group.Count(x => x.lote.Estado == "Activo"),
                LotesSinStock = group.Count(x => x.lote.Estado == "Activo" && x.lote.CantidadDisponible <= 0),
                CantidadInicial = group.Sum(x => x.lote.CantidadInicial),
                CantidadDisponible = group.Sum(x => x.lote.CantidadDisponible),
                UltimaFechaCreacion = group.Max(x => x.lote.FechaCreacion),
            })
            .OrderByDescending(x => x.CantidadDisponible)
            .ThenBy(x => x.ProductoNombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoteResponse>> GetAllAsync(int? productoId, bool soloActivos, bool soloConStock, CancellationToken cancellationToken)
    {
        var query = dbContext.Lotes.AsNoTracking().AsQueryable();

        if (productoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == productoId.Value);
        }

        if (soloActivos)
        {
            query = query.Where(x => x.Estado == "Activo");
        }

        if (soloConStock)
        {
            query = query.Where(x => x.CantidadDisponible > 0);
        }

        return await query
            .Join(
                dbContext.Productos.AsNoTracking(),
                lote => lote.ProductoId,
                producto => producto.ProductoId,
                (lote, producto) => new { lote, producto })
            .OrderBy(x => x.lote.FechaCreacion)
            .ThenBy(x => x.lote.LoteId)
            .Select(x => new LoteResponse
            {
                LoteId = x.lote.LoteId,
                ProductoId = x.lote.ProductoId,
                ProductoNombre = x.producto.Nombre,
                UnidadMedida = x.producto.UnidadMedida,
                CodigoLote = x.lote.CodigoLote,
                CantidadInicial = x.lote.CantidadInicial,
                CantidadDisponible = x.lote.CantidadDisponible,
                Estado = x.lote.Estado,
                FechaCreacion = x.lote.FechaCreacion,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LoteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Lotes
            .AsNoTracking()
            .Join(
                dbContext.Productos.AsNoTracking(),
                lote => lote.ProductoId,
                producto => producto.ProductoId,
                (lote, producto) => new { lote, producto })
            .Where(x => x.lote.LoteId == id)
            .Select(x => new LoteResponse
            {
                LoteId = x.lote.LoteId,
                ProductoId = x.lote.ProductoId,
                ProductoNombre = x.producto.Nombre,
                UnidadMedida = x.producto.UnidadMedida,
                CodigoLote = x.lote.CodigoLote,
                CantidadInicial = x.lote.CantidadInicial,
                CantidadDisponible = x.lote.CantidadDisponible,
                Estado = x.lote.Estado,
                FechaCreacion = x.lote.FechaCreacion,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<LoteTraceabilityResponse?> GetTraceabilityAsync(int id, CancellationToken cancellationToken)
    {
        var lote = await dbContext.Lotes
            .AsNoTracking()
            .Join(
                dbContext.Productos.AsNoTracking(),
                lote => lote.ProductoId,
                producto => producto.ProductoId,
                (lote, producto) => new { lote, producto })
            .Where(x => x.lote.LoteId == id)
            .Select(x => new LoteTraceabilityResponse
            {
                LoteId = x.lote.LoteId,
                ProductoId = x.lote.ProductoId,
                ProductoNombre = x.producto.Nombre,
                UnidadMedida = x.producto.UnidadMedida,
                CodigoLote = x.lote.CodigoLote,
                CantidadInicial = x.lote.CantidadInicial,
                CantidadDisponible = x.lote.CantidadDisponible,
                Estado = x.lote.Estado,
                FechaCreacion = x.lote.FechaCreacion,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (lote is null)
        {
            return null;
        }

        var cosechaOrigen = await (
            from cosecha in dbContext.CosechasCafe.AsNoTracking()
            join cultivo in dbContext.Cultivos.AsNoTracking() on cosecha.CultivoId equals cultivo.CultivoId
            join finca in dbContext.Fincas.AsNoTracking() on cultivo.FincaId equals finca.FincaId
            where cosecha.LoteId == id
            select new LoteTraceabilityOriginResponse
            {
                Tipo = "CosechaCafe",
                ReferenciaId = cosecha.CosechaCafeId,
                Fecha = cosecha.FechaCosecha,
                Titulo = $"Cosecha #{cosecha.CosechaCafeId}",
                Descripcion = $"Lote originado en {finca.Nombre} / {cultivo.Nombre}",
                CultivoId = cultivo.CultivoId,
                CultivoNombre = cultivo.Nombre,
                FincaNombre = finca.Nombre,
            })
            .SingleOrDefaultAsync(cancellationToken);

        var cosechaOperacionOrigen = cosechaOrigen is not null
            ? null
            : await (
                from operacion in dbContext.OperacionesAgricolas.AsNoTracking()
                join cultivo in dbContext.Cultivos.AsNoTracking() on operacion.CultivoId equals cultivo.CultivoId
                join finca in dbContext.Fincas.AsNoTracking() on cultivo.FincaId equals finca.FincaId
                where operacion.LoteId == id && operacion.TipoOperacion == TiposOperacionAgricola.Cosecha
                select new LoteTraceabilityOriginResponse
                {
                    Tipo = "CosechaCafe",
                    ReferenciaId = operacion.OperacionAgricolaId,
                    Fecha = operacion.FechaOperacion,
                    Titulo = $"Cosecha #{operacion.OperacionAgricolaId}",
                    Descripcion = $"Lote originado en {finca.Nombre} / {cultivo.Nombre}",
                    CultivoId = cultivo.CultivoId,
                    CultivoNombre = cultivo.Nombre,
                    FincaNombre = finca.Nombre,
                })
                .SingleOrDefaultAsync(cancellationToken);

        var recepcionOrigen = cosechaOrigen is not null || cosechaOperacionOrigen is not null
            ? null
            : await (
                from recepcion in dbContext.RecepcionesCafe.AsNoTracking()
                join proveedor in dbContext.Proveedores.AsNoTracking() on recepcion.ProveedorId equals proveedor.ProveedorId
                where recepcion.LoteId == id
                select new LoteTraceabilityOriginResponse
                {
                    Tipo = "RecepcionCafe",
                    ReferenciaId = recepcion.RecepcionCafeId,
                    Fecha = recepcion.FechaRecepcion,
                    Titulo = $"Recepción #{recepcion.RecepcionCafeId}",
                    Descripcion = $"Compra registrada a proveedor {proveedor.Nombre}",
                })
                .SingleOrDefaultAsync(cancellationToken);

        var procesoOrigen = cosechaOrigen is null && cosechaOperacionOrigen is null && recepcionOrigen is null
            ? await (
                from proceso in dbContext.ProcesosInventario.AsNoTracking()
                join recepcion in dbContext.RecepcionesCafe.AsNoTracking() on proceso.RecepcionCafeId equals recepcion.RecepcionCafeId
                where proceso.LoteSalidaId == id
                select new LoteTraceabilityOriginResponse
                {
                    Tipo = "ProcesoInventario",
                    ReferenciaId = proceso.ProcesoInventarioId,
                    Fecha = proceso.FechaProceso,
                    Titulo = $"Proceso #{proceso.ProcesoInventarioId}",
                    Descripcion = $"Lote generado desde la recepción {recepcion.CodigoLote}",
                    LoteOrigenId = recepcion.LoteId,
                    CodigoLoteOrigen = recepcion.CodigoLote,
                })
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        lote.Origen = cosechaOrigen
            ?? cosechaOperacionOrigen
            ?? recepcionOrigen
            ?? procesoOrigen
            ?? new LoteTraceabilityOriginResponse
            {
                Tipo = "Manual",
                ReferenciaId = null,
                Fecha = lote.FechaCreacion,
                Titulo = "Creación manual",
                Descripcion = "Lote creado manualmente en inventario.",
            };

        lote.Movimientos = await (
            from movimiento in dbContext.LoteMovimientos.AsNoTracking()
            join ventaRef in dbContext.Ventas.AsNoTracking() on movimiento.VentaId equals ventaRef.VentaId into ventas
            from venta in ventas.DefaultIfEmpty()
            join clienteRef in dbContext.Clientes.AsNoTracking() on venta.ClienteId equals clienteRef.ClienteId into clientes
            from cliente in clientes.DefaultIfEmpty()
            where movimiento.LoteId == id
            orderby movimiento.Fecha, movimiento.MovimientoId
            select new LoteTraceabilityMovementResponse
            {
                MovimientoId = movimiento.MovimientoId,
                Tipo = movimiento.Tipo,
                VentaId = movimiento.VentaId,
                ClienteNombre = cliente != null ? cliente.Nombre : null,
                VentaEstado = venta != null ? venta.Estado ?? true : null,
                Fecha = movimiento.Fecha,
                Cantidad = movimiento.Cantidad,
                Observacion = movimiento.Observacion,
            })
            .ToListAsync(cancellationToken);

        lote.ProcesosRelacionados = await (
            from proceso in dbContext.ProcesosInventario.AsNoTracking()
            join recepcion in dbContext.RecepcionesCafe.AsNoTracking() on proceso.RecepcionCafeId equals recepcion.RecepcionCafeId
            join productoOrigen in dbContext.Productos.AsNoTracking() on recepcion.ProductoId equals productoOrigen.ProductoId
            join loteSalida in dbContext.Lotes.AsNoTracking() on proceso.LoteSalidaId equals loteSalida.LoteId
            join productoSalida in dbContext.Productos.AsNoTracking() on proceso.ProductoSalidaId equals productoSalida.ProductoId
            where proceso.LoteSalidaId == id || proceso.LoteOrigenId == id
            orderby proceso.FechaProceso, proceso.ProcesoInventarioId
            select new LoteTraceabilityProcessResponse
            {
                ProcesoInventarioId = proceso.ProcesoInventarioId,
                Rol = proceso.LoteOrigenId == id ? "Origen" : "Salida",
                FechaProceso = proceso.FechaProceso,
                LoteOrigenId = recepcion.LoteId ?? 0,
                CodigoLoteOrigen = recepcion.CodigoLote,
                ProductoOrigenNombre = productoOrigen.Nombre,
                LoteSalidaId = loteSalida.LoteId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                ProductoSalidaNombre = productoSalida.Nombre,
                UsaLoteCompleto = proceso.UsaLoteCompleto,
                CantidadOrigenConsumida = proceso.CantidadOrigenConsumida,
                CantidadSalida = proceso.CantidadSalida,
                CostoProceso = proceso.CostoProceso,
                Estado = proceso.Estado,
            })
            .ToListAsync(cancellationToken);

        lote.CostosRelacionados = await dbContext.Costos
            .AsNoTracking()
            .Where(x => x.LoteId == id)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.CostoId)
            .Select(x => new LoteTraceabilityCostResponse
            {
                CostoId = x.CostoId,
                Nombre = x.Nombre,
                TipoCosto = x.TipoCosto,
                FuenteModulo = x.FuenteModulo,
                ReferenciaId = x.ReferenciaId,
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);

        if ((cosechaOrigen?.CultivoId ?? cosechaOperacionOrigen?.CultivoId) is int cultivoId)
        {
            var fechaLimite = cosechaOrigen?.Fecha ?? cosechaOperacionOrigen?.Fecha ?? lote.FechaCreacion;
            lote.OperacionesAgricolasRelacionadas = await (
                from operacion in dbContext.OperacionesAgricolas.AsNoTracking()
                join insumoRef in dbContext.Insumos.AsNoTracking() on operacion.InsumoId equals insumoRef.InsumoId into insumos
                from insumo in insumos.DefaultIfEmpty()
                where operacion.CultivoId == cultivoId
                    && operacion.TipoOperacion != TiposOperacionAgricola.Cosecha
                    && (operacion.FechaOperacion ?? fechaLimite) <= fechaLimite
                orderby operacion.FechaOperacion, operacion.OperacionAgricolaId
                select new LoteTraceabilityAgriculturalOperationResponse
                {
                    OperacionAgricolaId = operacion.OperacionAgricolaId,
                    TipoOperacion = operacion.TipoOperacion,
                    FechaOperacion = operacion.FechaOperacion,
                    InsumoNombre = insumo != null ? insumo.Nombre : null,
                    CantidadInsumo = operacion.CantidadInsumo,
                    CostoTotal = operacion.CostoTotal,
                    Estado = operacion.Estado,
                })
                .ToListAsync(cancellationToken);
        }

        return lote;
    }

    public async Task<int> CreateAsync(LoteRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductoId <= 0)
        {
            throw new ArgumentException("ProductoId inválido.", nameof(request));
        }

        if (request.CantidadInicial < 0)
        {
            throw new ArgumentException("CantidadInicial no puede ser negativa.", nameof(request));
        }

        var producto = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == request.ProductoId, cancellationToken);

        if (producto is null)
        {
            throw new ArgumentException("ProductoId inválido.", nameof(request));
        }

        producto.ManejaLotes = true;

        var tipoLote = NormalizeTipoLote(request.TipoLoteOverride) ?? NormalizeTipoLote(producto.TipoLote) ?? "Manual";
        var codigo = NormalizeCodigoLote(request.CodigoLote);
        if (codigo is null)
        {
            codigo = await AutoGenerarCodigoAsync(request.ProductoId, tipoLote, DateTime.Today, cancellationToken);
        }

        var estado = NormalizeEstado(request.Estado) ?? "Activo";

        var entity = new Lote
        {
            ProductoId = request.ProductoId,
            CodigoLote = codigo,
            CantidadInicial = request.CantidadInicial,
            CantidadDisponible = request.CantidadInicial,
            Estado = estado,
        };

        dbContext.Lotes.Add(entity);
        producto.Stock = (producto.Stock ?? 0m) + request.CantidadInicial;

        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = entity.LoteId,
            Tipo = "Ingreso",
            VentaId = null,
            Cantidad = request.CantidadInicial,
            Observacion = "Ingreso de inventario por lote",
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.LoteId;
    }

    public async Task<bool> UpdateAsync(int id, LoteUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var codigo = NormalizeCodigoLote(request.CodigoLote);
        if (codigo is not null)
        {
            entity.CodigoLote = codigo;
        }

        var estado = NormalizeEstado(request.Estado);
        if (estado is not null)
        {
            entity.Estado = estado;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var movimientos = await dbContext.LoteMovimientos
            .Where(x => x.LoteId == id)
            .ToListAsync(cancellationToken);

        if (movimientos.Count > 0)
        {
            dbContext.LoteMovimientos.RemoveRange(movimientos);
        }

        dbContext.Lotes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NormalizeCodigoLote(string? codigoLote)
    {
        var value = (codigoLote ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeTipoLote(string? tipoLote)
    {
        var value = (tipoLote ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeEstado(string? estado)
    {
        var value = (estado ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string BuildPolloCodigo(DateTime fecha, int correlativo)
        => $"POL-{fecha:yyyyMMdd}-{correlativo:000}";

    private static string BuildHuevoDiarioCodigo(DateTime fecha)
        => $"HUE-{fecha:yyyyMMdd}";

    private static string BuildHuevoSemanalCodigo(DateTime fecha)
    {
        var week = ISOWeek.GetWeekOfYear(fecha);
        var year = ISOWeek.GetYear(fecha);
        return $"HUE-{year}-W{week:00}";
    }

    private async Task<string> AutoGenerarCodigoAsync(int productoId, string tipoLote, DateTime fecha, CancellationToken cancellationToken)
    {
        if (tipoLote.Equals("PolloDiarioSecuencial", StringComparison.OrdinalIgnoreCase))
        {
            var d = fecha.Date;
            var prefix = $"POL-{d:yyyyMMdd}-";

            var correlativo = await dbContext.Lotes
                .AsNoTracking()
                .CountAsync(x => x.ProductoId == productoId && x.CodigoLote.StartsWith(prefix), cancellationToken);

            return BuildPolloCodigo(d, correlativo + 1);
        }

        if (tipoLote.Equals("HuevoDiario", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHuevoDiarioCodigo(fecha.Date);
        }

        if (tipoLote.Equals("HuevoSemanal", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHuevoSemanalCodigo(fecha.Date);
        }

        throw new ArgumentException("El código de lote es obligatorio para este producto (tipo Manual).", nameof(LoteRequest.CodigoLote));
    }
}
