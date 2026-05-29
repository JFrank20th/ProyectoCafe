using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Produccion.TransformacionesCafe;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Produccion.TransformacionesCafe;

public sealed class TransformacionCafeRepository(ProyectoCafeDbContext dbContext) : ITransformacionCafeRepository
{
    public async Task<IReadOnlyList<TransformacionCafeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await (
            from transformacion in dbContext.TransformacionesCafe.AsNoTracking()
            join loteOrigen in dbContext.Lotes.AsNoTracking() on transformacion.LoteOrigenId equals loteOrigen.LoteId
            join productoOrigen in dbContext.Productos.AsNoTracking() on loteOrigen.ProductoId equals productoOrigen.ProductoId
            join loteSalida in dbContext.Lotes.AsNoTracking() on transformacion.LoteSalidaId equals loteSalida.LoteId
            join productoSalida in dbContext.Productos.AsNoTracking() on transformacion.ProductoSalidaId equals productoSalida.ProductoId
            orderby transformacion.FechaTransformacion descending, transformacion.TransformacionCafeId descending
            select new TransformacionCafeResponse
            {
                TransformacionCafeId = transformacion.TransformacionCafeId,
                LoteOrigenId = transformacion.LoteOrigenId,
                CodigoLoteOrigen = loteOrigen.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = productoOrigen.UnidadMedida,
                ProductoSalidaId = transformacion.ProductoSalidaId,
                ProductoSalidaNombre = productoSalida.Nombre,
                UnidadMedidaSalida = productoSalida.UnidadMedida,
                LoteSalidaId = transformacion.LoteSalidaId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                TipoTransformacion = transformacion.TipoTransformacion,
                FechaTransformacion = transformacion.FechaTransformacion,
                CantidadEntrada = transformacion.CantidadEntrada,
                CantidadSalida = transformacion.CantidadSalida,
                CostoProceso = transformacion.CostoProceso,
                Merma = transformacion.Merma,
                Rendimiento = transformacion.Rendimiento,
                Observaciones = transformacion.Observaciones,
                Estado = transformacion.Estado,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TransformacionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from transformacion in dbContext.TransformacionesCafe.AsNoTracking()
            join loteOrigen in dbContext.Lotes.AsNoTracking() on transformacion.LoteOrigenId equals loteOrigen.LoteId
            join productoOrigen in dbContext.Productos.AsNoTracking() on loteOrigen.ProductoId equals productoOrigen.ProductoId
            join loteSalida in dbContext.Lotes.AsNoTracking() on transformacion.LoteSalidaId equals loteSalida.LoteId
            join productoSalida in dbContext.Productos.AsNoTracking() on transformacion.ProductoSalidaId equals productoSalida.ProductoId
            where transformacion.TransformacionCafeId == id
            select new TransformacionCafeResponse
            {
                TransformacionCafeId = transformacion.TransformacionCafeId,
                LoteOrigenId = transformacion.LoteOrigenId,
                CodigoLoteOrigen = loteOrigen.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = productoOrigen.UnidadMedida,
                ProductoSalidaId = transformacion.ProductoSalidaId,
                ProductoSalidaNombre = productoSalida.Nombre,
                UnidadMedidaSalida = productoSalida.UnidadMedida,
                LoteSalidaId = transformacion.LoteSalidaId,
                CodigoLoteSalida = loteSalida.CodigoLote,
                TipoTransformacion = transformacion.TipoTransformacion,
                FechaTransformacion = transformacion.FechaTransformacion,
                CantidadEntrada = transformacion.CantidadEntrada,
                CantidadSalida = transformacion.CantidadSalida,
                CostoProceso = transformacion.CostoProceso,
                Merma = transformacion.Merma,
                Rendimiento = transformacion.Rendimiento,
                Observaciones = transformacion.Observaciones,
                Estado = transformacion.Estado,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TransformacionCafeCreateResult> CreateAsync(TransformacionCafeCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.LoteOrigenId <= 0)
        {
            throw new ArgumentException("LoteOrigenId inválido.", nameof(request));
        }

        if (request.ProductoSalidaId <= 0)
        {
            throw new ArgumentException("ProductoSalidaId inválido.", nameof(request));
        }

        var tipoTransformacion = NormalizeTransformacion(request.TipoTransformacion);
        if (tipoTransformacion is null)
        {
            throw new ArgumentException("TipoTransformacion es requerido.", nameof(request));
        }

        if (request.CantidadEntrada <= 0)
        {
            throw new ArgumentException("CantidadEntrada debe ser mayor a 0.", nameof(request));
        }

        if (request.CantidadSalida <= 0)
        {
            throw new ArgumentException("CantidadSalida debe ser mayor a 0.", nameof(request));
        }

        if (request.CostoProceso < 0)
        {
            throw new ArgumentException("CostoProceso no puede ser negativo.", nameof(request));
        }

        var loteOrigen = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == request.LoteOrigenId, cancellationToken);

        if (loteOrigen is null)
        {
            throw new ArgumentException("LoteOrigenId inválido.", nameof(request));
        }

        if (!loteOrigen.Estado.Equals("Activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El lote origen debe estar activo.", nameof(request));
        }

        if (loteOrigen.CantidadDisponible < request.CantidadEntrada)
        {
            throw new ArgumentException("CantidadEntrada supera el disponible del lote origen.", nameof(request));
        }

        var productoOrigen = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == loteOrigen.ProductoId, cancellationToken);

        if (productoOrigen is null || productoOrigen.Estado == false)
        {
            throw new ArgumentException("El producto origen no existe o está inactivo.", nameof(request));
        }

        var productoSalida = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == request.ProductoSalidaId, cancellationToken);

        if (productoSalida is null || productoSalida.Estado == false)
        {
            throw new ArgumentException("ProductoSalidaId inválido o inactivo.", nameof(request));
        }

        if (!productoOrigen.PermiteDecimal && request.CantidadEntrada != decimal.Truncate(request.CantidadEntrada))
        {
            throw new ArgumentException($"ProductoId {productoOrigen.ProductoId} no permite cantidad decimal.", nameof(request));
        }

        if (!productoSalida.PermiteDecimal && request.CantidadSalida != decimal.Truncate(request.CantidadSalida))
        {
            throw new ArgumentException($"ProductoSalidaId {productoSalida.ProductoId} no permite cantidad decimal.", nameof(request));
        }

        var mismaUnidad = string.Equals(productoOrigen.UnidadMedida, productoSalida.UnidadMedida, StringComparison.OrdinalIgnoreCase);
        if (mismaUnidad && request.CantidadSalida > request.CantidadEntrada)
        {
            throw new ArgumentException("CantidadSalida no puede exceder CantidadEntrada cuando las unidades coinciden.", nameof(request));
        }

        var merma = mismaUnidad
            ? Math.Round(request.CantidadEntrada - request.CantidadSalida, 3, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        var rendimiento = mismaUnidad
            ? Math.Round((request.CantidadSalida / request.CantidadEntrada) * 100m, 4, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        var fechaBase = (request.FechaTransformacion ?? DateTime.Today).Date;
        var codigoLoteSalida = NormalizeCode(request.CodigoLoteSalida) ?? await AutoGenerarCodigoSalidaAsync(tipoTransformacion, request.ProductoSalidaId, fechaBase, cancellationToken);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            loteOrigen.CantidadDisponible -= request.CantidadEntrada;
            productoOrigen.Stock = (productoOrigen.Stock ?? 0m) - request.CantidadEntrada;

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

            var transformacion = new TransformacionCafe
            {
                LoteOrigenId = request.LoteOrigenId,
                ProductoSalidaId = request.ProductoSalidaId,
                LoteSalidaId = loteSalida.LoteId,
                TipoTransformacion = tipoTransformacion,
                FechaTransformacion = request.FechaTransformacion,
                CantidadEntrada = request.CantidadEntrada,
                CantidadSalida = request.CantidadSalida,
                CostoProceso = request.CostoProceso,
                Merma = merma,
                Rendimiento = rendimiento,
                Observaciones = NormalizeText(request.Observaciones),
                Estado = "Registrada",
            };

            dbContext.TransformacionesCafe.Add(transformacion);

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = loteOrigen.LoteId,
                Tipo = "TransformacionSalida",
                VentaId = null,
                Cantidad = request.CantidadEntrada,
                Observacion = $"Salida por {tipoTransformacion}",
            });

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = loteSalida.LoteId,
                Tipo = "TransformacionEntrada",
                VentaId = null,
                Cantidad = request.CantidadSalida,
                Observacion = $"Ingreso por {tipoTransformacion}",
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            if (request.CostoProceso > 0)
            {
                dbContext.Costos.Add(new Costo
                {
                    Nombre = $"Costo transformación {loteSalida.CodigoLote}",
                    TipoCosto = TiposCostoContable.TransformacionCafe,
                    FuenteModulo = "Produccion.TransformacionesCafe",
                    ReferenciaId = transformacion.TransformacionCafeId,
                    ParcelaId = null,
                    LoteId = loteSalida.LoteId,
                    Monto = request.CostoProceso,
                    Fecha = request.FechaTransformacion ?? fechaBase,
                    Estado = true,
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return new TransformacionCafeCreateResult
            {
                TransformacionCafeId = transformacion.TransformacionCafeId,
                LoteSalidaId = loteSalida.LoteId,
                CodigoLoteSalida = loteSalida.CodigoLote,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar la transformación. Verifica que el código del lote salida no esté duplicado.", nameof(request), ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var transformacion = await dbContext.TransformacionesCafe
            .SingleOrDefaultAsync(x => x.TransformacionCafeId == id, cancellationToken);

        if (transformacion is null)
        {
            return false;
        }

        if (transformacion.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var loteOrigen = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == transformacion.LoteOrigenId, cancellationToken);
        var loteSalida = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == transformacion.LoteSalidaId, cancellationToken);
        var productoOrigen = loteOrigen is null
            ? null
            : await dbContext.Productos.SingleOrDefaultAsync(x => x.ProductoId == loteOrigen.ProductoId, cancellationToken);
        var productoSalida = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == transformacion.ProductoSalidaId, cancellationToken);

        if (loteOrigen is null || loteSalida is null || productoOrigen is null || productoSalida is null)
        {
            throw new ArgumentException("La transformación tiene relaciones incompletas y no puede anularse.", nameof(id));
        }

        if (loteSalida.CantidadDisponible != transformacion.CantidadSalida || loteSalida.CantidadInicial != transformacion.CantidadSalida)
        {
            throw new ArgumentException("No se puede anular porque el lote salida ya tuvo movimientos posteriores.", nameof(id));
        }

        if ((productoSalida.Stock ?? 0m) < transformacion.CantidadSalida)
        {
            throw new ArgumentException("No se puede anular porque el stock actual del producto salida es insuficiente.", nameof(id));
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        loteOrigen.CantidadDisponible += transformacion.CantidadEntrada;
        productoOrigen.Stock = (productoOrigen.Stock ?? 0m) + transformacion.CantidadEntrada;

        loteSalida.CantidadDisponible = 0m;
        loteSalida.Estado = "Inactivo";
        productoSalida.Stock = (productoSalida.Stock ?? 0m) - transformacion.CantidadSalida;
        transformacion.Estado = "Anulada";

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = loteOrigen.LoteId,
            Tipo = "AnulacionTransformacionRetorno",
            VentaId = null,
            Cantidad = transformacion.CantidadEntrada,
            Observacion = $"Anulación transformación #{transformacion.TransformacionCafeId}",
        });

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = loteSalida.LoteId,
            Tipo = "AnulacionTransformacionSalida",
            VentaId = null,
            Cantidad = transformacion.CantidadSalida,
            Observacion = $"Anulación transformación #{transformacion.TransformacionCafeId}",
        });

        var costos = await dbContext.Costos
            .Where(x => x.FuenteModulo == "Produccion.TransformacionesCafe" && x.ReferenciaId == transformacion.TransformacionCafeId && x.Estado != false)
            .ToListAsync(cancellationToken);

        foreach (var costo in costos)
        {
            costo.Estado = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<string> AutoGenerarCodigoSalidaAsync(string tipoTransformacion, int productoSalidaId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"TRF-{MapTypePrefix(tipoTransformacion)}-{productoSalidaId:000}-{fecha:yyyyMMdd}-";
        var correlativo = await dbContext.Lotes
            .AsNoTracking()
            .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);
        return $"{prefix}{correlativo + 1:000}";
    }

    private static string? NormalizeTransformacion(string? tipoTransformacion)
    {
        var normalized = (tipoTransformacion ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string MapTypePrefix(string tipoTransformacion)
    {
        var compact = new string(tipoTransformacion
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (compact.Length >= 3)
        {
            return compact[..3];
        }

        return compact.PadRight(3, 'X');
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
