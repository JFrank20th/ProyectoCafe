using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Inventario.ProcesosInventario;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.ProcesosInventario;

public sealed class ProcesoInventarioRepository(ProyectoCafeDbContext dbContext) : IProcesoInventarioRepository
{
    public async Task<IReadOnlyList<ProcesoInventarioResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await (
            from proceso in dbContext.ProcesosInventario.AsNoTracking()
            join recepcion in dbContext.RecepcionesCafe.AsNoTracking() on proceso.RecepcionCafeId equals recepcion.RecepcionCafeId
            join productoOrigen in dbContext.Productos.AsNoTracking() on recepcion.ProductoId equals productoOrigen.ProductoId
            join loteSalida in dbContext.Lotes.AsNoTracking() on proceso.LoteSalidaId equals loteSalida.LoteId
            join productoSalida in dbContext.Productos.AsNoTracking() on proceso.ProductoSalidaId equals productoSalida.ProductoId
            orderby proceso.FechaProceso descending, proceso.ProcesoInventarioId descending
            select new ProcesoInventarioResponse
            {
                ProcesoInventarioId = proceso.ProcesoInventarioId,
                RecepcionCafeId = proceso.RecepcionCafeId,
                LoteOrigenId = proceso.LoteOrigenId,
                CodigoLoteOrigen = recepcion.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = "KG",
                ProductoSalidaId = proceso.ProductoSalidaId,
                ProductoSalidaNombre = productoSalida.Nombre,
                UnidadMedidaSalida = productoSalida.UnidadMedida,
                LoteSalidaId = proceso.LoteSalidaId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                UsaLoteCompleto = proceso.UsaLoteCompleto,
                FechaProceso = proceso.FechaProceso,
                CantidadOrigenConsumida = proceso.CantidadOrigenConsumida,
                CantidadSalida = proceso.CantidadSalida,
                CostoProceso = proceso.CostoProceso,
                Observaciones = proceso.Observaciones,
                Estado = proceso.Estado,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProcesoInventarioResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from proceso in dbContext.ProcesosInventario.AsNoTracking()
            join recepcion in dbContext.RecepcionesCafe.AsNoTracking() on proceso.RecepcionCafeId equals recepcion.RecepcionCafeId
            join productoOrigen in dbContext.Productos.AsNoTracking() on recepcion.ProductoId equals productoOrigen.ProductoId
            join loteSalida in dbContext.Lotes.AsNoTracking() on proceso.LoteSalidaId equals loteSalida.LoteId
            join productoSalida in dbContext.Productos.AsNoTracking() on proceso.ProductoSalidaId equals productoSalida.ProductoId
            where proceso.ProcesoInventarioId == id
            select new ProcesoInventarioResponse
            {
                ProcesoInventarioId = proceso.ProcesoInventarioId,
                RecepcionCafeId = proceso.RecepcionCafeId,
                LoteOrigenId = proceso.LoteOrigenId,
                CodigoLoteOrigen = recepcion.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = "KG",
                ProductoSalidaId = proceso.ProductoSalidaId,
                ProductoSalidaNombre = productoSalida.Nombre,
                UnidadMedidaSalida = productoSalida.UnidadMedida,
                LoteSalidaId = proceso.LoteSalidaId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                UsaLoteCompleto = proceso.UsaLoteCompleto,
                FechaProceso = proceso.FechaProceso,
                CantidadOrigenConsumida = proceso.CantidadOrigenConsumida,
                CantidadSalida = proceso.CantidadSalida,
                CostoProceso = proceso.CostoProceso,
                Observaciones = proceso.Observaciones,
                Estado = proceso.Estado,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProcesoInventarioCreateResult> CreateAsync(ProcesoInventarioCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.RecepcionCafeId <= 0)
        {
            throw new ArgumentException("RecepcionCafeId inválido.", nameof(request));
        }

        if (request.ProductoSalidaId <= 0)
        {
            throw new ArgumentException("ProductoSalidaId inválido.", nameof(request));
        }

        if (request.CantidadSalida <= 0)
        {
            throw new ArgumentException("CantidadSalida debe ser mayor a 0.", nameof(request));
        }

        if (request.CostoProceso < 0)
        {
            throw new ArgumentException("CostoProceso no puede ser negativo.", nameof(request));
        }

        var origen = await (
            from recepcion in dbContext.RecepcionesCafe
            join producto in dbContext.Productos on recepcion.ProductoId equals producto.ProductoId
            where recepcion.RecepcionCafeId == request.RecepcionCafeId
            select new
            {
                Recepcion = recepcion,
                Producto = producto,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (origen is null)
        {
            throw new ArgumentException("RecepcionCafeId inválido.", nameof(request));
        }

        if (!origen.Recepcion.Estado.Equals("Registrada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La recepción origen debe estar registrada.", nameof(request));
        }

        if (origen.Producto.Estado == false)
        {
            throw new ArgumentException("El producto origen está inactivo.", nameof(request));
        }

        var productoSalida = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == request.ProductoSalidaId, cancellationToken);

        if (productoSalida is null || productoSalida.Estado == false)
        {
            throw new ArgumentException("ProductoSalidaId inválido o inactivo.", nameof(request));
        }

        var cantidadOrigenConsumida = request.UsaLoteCompleto
            ? origen.Recepcion.CantidadDisponible
            : request.CantidadOrigenConsumida ?? 0m;

        if (cantidadOrigenConsumida <= 0)
        {
            throw new ArgumentException("CantidadOrigenConsumida debe ser mayor a 0.", nameof(request));
        }

        if (origen.Recepcion.CantidadDisponible < cantidadOrigenConsumida)
        {
            throw new ArgumentException("CantidadOrigenConsumida supera el disponible de la recepción origen.", nameof(request));
        }

        var fechaBase = (request.FechaProceso ?? DateTime.Today).Date;
        var codigoLoteSalida = NormalizeCode(request.CodigoLoteSalida)
            ?? await AutoGenerarCodigoSalidaAsync(request.ProductoSalidaId, fechaBase, cancellationToken);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            origen.Recepcion.CantidadDisponible -= cantidadOrigenConsumida;

            var loteSalida = new Lote
            {
                ProductoId = request.ProductoSalidaId,
                CodigoLote = codigoLoteSalida,
                CantidadInicial = request.CantidadSalida,
                CantidadDisponible = request.CantidadSalida,
                Estado = "Activo",
            };

            dbContext.Lotes.Add(loteSalida);
            productoSalida.ManejaLotes = true;
            productoSalida.Stock = (productoSalida.Stock ?? 0m) + request.CantidadSalida;

            await dbContext.SaveChangesAsync(cancellationToken);

            var proceso = new ProcesoInventario
            {
                RecepcionCafeId = origen.Recepcion.RecepcionCafeId,
                LoteOrigenId = null,
                ProductoSalidaId = productoSalida.ProductoId,
                LoteSalidaId = loteSalida.LoteId,
                UsaLoteCompleto = request.UsaLoteCompleto,
                FechaProceso = request.FechaProceso,
                CantidadOrigenConsumida = cantidadOrigenConsumida,
                CantidadSalida = request.CantidadSalida,
                CostoProceso = request.CostoProceso,
                Observaciones = NormalizeText(request.Observaciones),
                Estado = "Registrado",
            };

            dbContext.ProcesosInventario.Add(proceso);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = loteSalida.LoteId,
                Tipo = "ProcesoEntrada",
                VentaId = null,
                Cantidad = request.CantidadSalida,
                Observacion = $"Ingreso por proceso #{proceso.ProcesoInventarioId}",
            });

            if (request.CostoProceso > 0)
            {
                dbContext.Costos.Add(new Costo
                {
                    Nombre = $"Costo proceso {loteSalida.CodigoLote}",
                    TipoCosto = TiposCostoContable.ProcesoInventario,
                    FuenteModulo = "Inventario.ProcesosInventario",
                    ReferenciaId = proceso.ProcesoInventarioId,
                    LoteId = loteSalida.LoteId,
                    Monto = request.CostoProceso,
                    Fecha = request.FechaProceso ?? fechaBase,
                    Estado = true,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new ProcesoInventarioCreateResult
            {
                ProcesoInventarioId = proceso.ProcesoInventarioId,
                LoteSalidaId = loteSalida.LoteId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                CantidadOrigenConsumida = cantidadOrigenConsumida,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar el proceso. Verifica que el código del lote salida no esté duplicado.", nameof(request), ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var proceso = await dbContext.ProcesosInventario
            .SingleOrDefaultAsync(x => x.ProcesoInventarioId == id, cancellationToken);

        if (proceso is null)
        {
            return false;
        }

        if (proceso.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var recepcion = await dbContext.RecepcionesCafe
            .SingleOrDefaultAsync(x => x.RecepcionCafeId == proceso.RecepcionCafeId, cancellationToken);
        var loteSalida = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == proceso.LoteSalidaId, cancellationToken);
        var productoSalida = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == proceso.ProductoSalidaId, cancellationToken);

        if (recepcion is null || loteSalida is null || productoSalida is null)
        {
            throw new ArgumentException("El proceso tiene relaciones incompletas y no puede anularse.", nameof(id));
        }

        if (loteSalida.CantidadDisponible != proceso.CantidadSalida || loteSalida.CantidadInicial != proceso.CantidadSalida)
        {
            throw new ArgumentException("No se puede anular porque el lote salida ya tuvo movimientos posteriores.", nameof(id));
        }

        if ((productoSalida.Stock ?? 0m) < proceso.CantidadSalida)
        {
            throw new ArgumentException("No se puede anular porque el stock actual del producto salida es insuficiente.", nameof(id));
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        recepcion.CantidadDisponible += proceso.CantidadOrigenConsumida;

        loteSalida.CantidadDisponible = 0m;
        loteSalida.Estado = "Inactivo";
        productoSalida.Stock = (productoSalida.Stock ?? 0m) - proceso.CantidadSalida;
        proceso.Estado = "Anulado";

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = loteSalida.LoteId,
            Tipo = "ProcesoSalida",
            VentaId = null,
            Cantidad = proceso.CantidadSalida,
            Observacion = $"Anulación proceso #{proceso.ProcesoInventarioId}",
        });

        var costos = await dbContext.Costos
            .Where(x => x.FuenteModulo == "Inventario.ProcesosInventario" && x.ReferenciaId == proceso.ProcesoInventarioId && x.Estado != false)
            .ToListAsync(cancellationToken);

        foreach (var costo in costos)
        {
            costo.Estado = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<string> AutoGenerarCodigoSalidaAsync(int productoSalidaId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"PRC-{productoSalidaId:000}-{fecha:yyyyMMdd}-";
        var correlativo = await dbContext.Lotes
            .AsNoTracking()
            .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);
        return $"{prefix}{correlativo + 1:000}";
    }

    private static string? NormalizeCode(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}