using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Produccion.OperacionesAgricolas;

public sealed class OperacionAgricolaRepository(ProyectoCafeDbContext dbContext) : IOperacionAgricolaRepository
{
    public async Task<IReadOnlyList<OperacionAgricolaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await (
            from operacion in dbContext.OperacionesAgricolas.AsNoTracking()
            join parcela in dbContext.Parcelas.AsNoTracking() on operacion.ParcelaId equals parcela.ParcelaId
            join finca in dbContext.Fincas.AsNoTracking() on parcela.FincaId equals finca.FincaId
            join insumoRef in dbContext.Insumos.AsNoTracking() on operacion.InsumoId equals insumoRef.InsumoId into insumos
            from insumo in insumos.DefaultIfEmpty()
            join productoRef in dbContext.Productos.AsNoTracking() on operacion.ProductoId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            join loteRef in dbContext.Lotes.AsNoTracking() on operacion.LoteId equals loteRef.LoteId into lotes
            from lote in lotes.DefaultIfEmpty()
            orderby operacion.FechaOperacion descending, operacion.OperacionAgricolaId descending
            select new OperacionAgricolaResponse
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                ParcelaId = operacion.ParcelaId,
                ParcelaNombre = parcela.Nombre,
                FincaId = finca.FincaId,
                FincaNombre = finca.Nombre,
                TipoOperacion = operacion.TipoOperacion,
                FechaOperacion = operacion.FechaOperacion,
                InsumoId = operacion.InsumoId,
                InsumoNombre = insumo != null ? insumo.Nombre : null,
                ProductoId = operacion.ProductoId,
                ProductoNombre = producto != null ? producto.Nombre : null,
                CantidadInsumo = operacion.CantidadInsumo,
                Cantidad = operacion.Cantidad,
                Cultivo = operacion.Cultivo,
                Humedad = operacion.Humedad,
                LoteId = operacion.LoteId,
                CodigoLote = lote != null ? lote.CodigoLote : null,
                CostoInsumo = operacion.CostoInsumo,
                CostoManoObra = operacion.CostoManoObra,
                CostoOtros = operacion.CostoOtros,
                CostoTotal = operacion.CostoTotal,
                Observaciones = operacion.Observaciones,
                Estado = operacion.Estado,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperacionAgricolaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from operacion in dbContext.OperacionesAgricolas.AsNoTracking()
            join parcela in dbContext.Parcelas.AsNoTracking() on operacion.ParcelaId equals parcela.ParcelaId
            join finca in dbContext.Fincas.AsNoTracking() on parcela.FincaId equals finca.FincaId
            join insumoRef in dbContext.Insumos.AsNoTracking() on operacion.InsumoId equals insumoRef.InsumoId into insumos
            from insumo in insumos.DefaultIfEmpty()
            join productoRef in dbContext.Productos.AsNoTracking() on operacion.ProductoId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            join loteRef in dbContext.Lotes.AsNoTracking() on operacion.LoteId equals loteRef.LoteId into lotes
            from lote in lotes.DefaultIfEmpty()
            where operacion.OperacionAgricolaId == id
            select new OperacionAgricolaResponse
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                ParcelaId = operacion.ParcelaId,
                ParcelaNombre = parcela.Nombre,
                FincaId = finca.FincaId,
                FincaNombre = finca.Nombre,
                TipoOperacion = operacion.TipoOperacion,
                FechaOperacion = operacion.FechaOperacion,
                InsumoId = operacion.InsumoId,
                InsumoNombre = insumo != null ? insumo.Nombre : null,
                ProductoId = operacion.ProductoId,
                ProductoNombre = producto != null ? producto.Nombre : null,
                CantidadInsumo = operacion.CantidadInsumo,
                Cantidad = operacion.Cantidad,
                Cultivo = operacion.Cultivo,
                Humedad = operacion.Humedad,
                LoteId = operacion.LoteId,
                CodigoLote = lote != null ? lote.CodigoLote : null,
                CostoInsumo = operacion.CostoInsumo,
                CostoManoObra = operacion.CostoManoObra,
                CostoOtros = operacion.CostoOtros,
                CostoTotal = operacion.CostoTotal,
                Observaciones = operacion.Observaciones,
                Estado = operacion.Estado,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<OperacionAgricolaCreateResult> CreateAsync(OperacionAgricolaCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.ParcelaId <= 0)
        {
            throw new ArgumentException("ParcelaId inválido.", nameof(request));
        }

        var tipoOperacion = TiposOperacionAgricola.Normalize(request.TipoOperacion);
        if (tipoOperacion is null)
        {
            throw new ArgumentException("TipoOperacion inválido.", nameof(request));
        }

        var esCosecha = IsHarvestOperation(tipoOperacion);

        if (request.CostoManoObra < 0 || request.CostoOtros < 0)
        {
            throw new ArgumentException("Los costos no pueden ser negativos.", nameof(request));
        }

        if (request.CantidadInsumo.HasValue && request.CantidadInsumo.Value < 0)
        {
            throw new ArgumentException("CantidadInsumo no puede ser negativa.", nameof(request));
        }

        var parcela = await dbContext.Parcelas
            .SingleOrDefaultAsync(x => x.ParcelaId == request.ParcelaId, cancellationToken);

        if (parcela is null || parcela.Estado == false)
        {
            throw new ArgumentException("ParcelaId inválido o inactivo.", nameof(request));
        }

        var finca = await dbContext.Fincas
            .SingleOrDefaultAsync(x => x.FincaId == parcela.FincaId, cancellationToken);

        if (finca is null || finca.Estado == false)
        {
            throw new ArgumentException("La finca asociada a la parcela no existe o está inactiva.", nameof(request));
        }

        if (esCosecha)
        {
            return await CreateHarvestAsync(request, tipoOperacion, parcela, cancellationToken);
        }

        Insumo? insumo = null;
        decimal? cantidadInsumo = null;
        decimal costoInsumo = 0m;

        if (request.InsumoId.HasValue)
        {
            if (!request.CantidadInsumo.HasValue || request.CantidadInsumo.Value <= 0)
            {
                throw new ArgumentException("CantidadInsumo debe ser mayor a 0 cuando se usa un insumo.", nameof(request));
            }

            insumo = await dbContext.Insumos
                .SingleOrDefaultAsync(x => x.InsumoId == request.InsumoId.Value, cancellationToken);

            if (insumo is null || insumo.Estado == false)
            {
                throw new ArgumentException("InsumoId inválido o inactivo.", nameof(request));
            }

            var disponible = insumo.Cantidad ?? 0m;
            if (disponible < request.CantidadInsumo.Value)
            {
                throw new ArgumentException("La cantidad del insumo es insuficiente para registrar la operación.", nameof(request));
            }

            cantidadInsumo = request.CantidadInsumo.Value;
            costoInsumo = Math.Round(cantidadInsumo.Value * (insumo.CostoUnitario ?? 0m), 2, MidpointRounding.AwayFromZero);
        }
        else if (request.CantidadInsumo.HasValue && request.CantidadInsumo.Value > 0)
        {
            throw new ArgumentException("No se puede informar CantidadInsumo sin seleccionar un insumo.", nameof(request));
        }

        var costoTotal = Math.Round(costoInsumo + request.CostoManoObra + request.CostoOtros, 2, MidpointRounding.AwayFromZero);
        var fechaBase = request.FechaOperacion ?? DateTime.Now;

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (insumo is not null && cantidadInsumo.HasValue)
            {
                insumo.Cantidad = (insumo.Cantidad ?? 0m) - cantidadInsumo.Value;
            }

            var operacion = new OperacionAgricola
            {
                ParcelaId = request.ParcelaId,
                TipoOperacion = tipoOperacion,
                FechaOperacion = request.FechaOperacion,
                InsumoId = request.InsumoId,
                ProductoId = null,
                CantidadInsumo = cantidadInsumo,
                Cantidad = null,
                Cultivo = null,
                Humedad = null,
                LoteId = null,
                CostoInsumo = costoInsumo,
                CostoManoObra = request.CostoManoObra,
                CostoOtros = request.CostoOtros,
                CostoTotal = costoTotal,
                Observaciones = NormalizeText(request.Observaciones),
                Estado = "Registrada",
            };

            dbContext.OperacionesAgricolas.Add(operacion);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (costoTotal > 0)
            {
                dbContext.Costos.Add(new Costo
                {
                    Nombre = $"Costo operación {tipoOperacion} - parcela {parcela.Nombre}",
                    TipoCosto = TiposCostoContable.OperacionAgricola,
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = operacion.OperacionAgricolaId,
                    ParcelaId = operacion.ParcelaId,
                    LoteId = null,
                    Monto = costoTotal,
                    Fecha = fechaBase,
                    Estado = true,
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return new OperacionAgricolaCreateResult
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                LoteId = null,
                CodigoLote = null,
                CostoTotal = costoTotal,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar la operación agrícola.", nameof(request), ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var operacion = await dbContext.OperacionesAgricolas
            .SingleOrDefaultAsync(x => x.OperacionAgricolaId == id, cancellationToken);

        if (operacion is null)
        {
            return false;
        }

        if (operacion.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (IsHarvestOperation(operacion.TipoOperacion))
        {
            await CancelHarvestAsync(operacion, cancellationToken);
        }
        else if (operacion.InsumoId.HasValue && operacion.CantidadInsumo.HasValue && operacion.CantidadInsumo.Value > 0)
        {
            var insumo = await dbContext.Insumos
                .SingleOrDefaultAsync(x => x.InsumoId == operacion.InsumoId.Value, cancellationToken);

            if (insumo is null)
            {
                throw new ArgumentException("El insumo asociado no existe.", nameof(id));
            }

            insumo.Cantidad = (insumo.Cantidad ?? 0m) + operacion.CantidadInsumo.Value;
        }

        operacion.Estado = "Anulada";

        var costos = await dbContext.Costos
            .Where(x => x.FuenteModulo == "Produccion.OperacionesAgricolas" && x.ReferenciaId == operacion.OperacionAgricolaId && x.Estado != false)
            .ToListAsync(cancellationToken);

        foreach (var costo in costos)
        {
            costo.Estado = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<OperacionAgricolaCreateResult> CreateHarvestAsync(
        OperacionAgricolaCreateRequest request,
        string tipoOperacion,
        Parcela parcela,
        CancellationToken cancellationToken)
    {
        if (parcela.ProductoCosechaId is not int productoId)
        {
            throw new ArgumentException("La parcela no tiene un producto de cosecha configurado.", nameof(request));
        }

        if (request.Cantidad is not decimal cantidad || cantidad <= 0)
        {
            throw new ArgumentException("Cantidad debe ser mayor a 0.", nameof(request));
        }

        if (request.InsumoId.HasValue || request.CantidadInsumo.HasValue)
        {
            throw new ArgumentException("La cosecha no admite insumos asociados.", nameof(request));
        }

        var producto = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == productoId, cancellationToken);

        if (producto is null || producto.Estado == false)
        {
            throw new ArgumentException("ProductoId inválido o inactivo.", nameof(request));
        }

        if (!producto.PermiteDecimal && cantidad != decimal.Truncate(cantidad))
        {
            throw new ArgumentException($"ProductoId {productoId} no permite cantidad decimal.", nameof(request));
        }

        var cultivo = NormalizeCultivo(parcela.Cultivo)
            ?? NormalizeCultivo(producto.Nombre)
            ?? "Cosecha";
        var fechaBase = (request.FechaOperacion ?? DateTime.Today).Date;
        var codigoLote = await AutoGenerateHarvestLotCodeAsync(cultivo, request.ParcelaId, productoId, fechaBase, cancellationToken);
        var costoTotal = Math.Round(request.CostoManoObra + request.CostoOtros, 2, MidpointRounding.AwayFromZero);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var lote = new Lote
            {
                ProductoId = productoId,
                CodigoLote = codigoLote,
                CantidadInicial = cantidad,
                CantidadDisponible = cantidad,
                Estado = "Activo",
            };

            dbContext.Lotes.Add(lote);
            producto.ManejaLotes = true;
            producto.Stock = (producto.Stock ?? 0m) + cantidad;

            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = lote.LoteId,
                Tipo = "IngresoCosecha",
                VentaId = null,
                Cantidad = cantidad,
                Observacion = $"Ingreso por cosecha en parcela #{parcela.ParcelaId}",
            });

            var operacion = new OperacionAgricola
            {
                ParcelaId = request.ParcelaId,
                TipoOperacion = tipoOperacion,
                FechaOperacion = request.FechaOperacion,
                InsumoId = null,
                ProductoId = productoId,
                CantidadInsumo = null,
                Cantidad = cantidad,
                Cultivo = cultivo,
                Humedad = null,
                LoteId = lote.LoteId,
                CostoInsumo = 0m,
                CostoManoObra = request.CostoManoObra,
                CostoOtros = request.CostoOtros,
                CostoTotal = costoTotal,
                Observaciones = NormalizeText(request.Observaciones),
                Estado = "Registrada",
            };

            dbContext.OperacionesAgricolas.Add(operacion);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (costoTotal > 0)
            {
                dbContext.Costos.Add(new Costo
                {
                    Nombre = $"Costo cosecha {lote.CodigoLote}",
                    TipoCosto = TiposCostoContable.CosechaCafe,
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = operacion.OperacionAgricolaId,
                    ParcelaId = operacion.ParcelaId,
                    LoteId = lote.LoteId,
                    Monto = costoTotal,
                    Fecha = request.FechaOperacion ?? fechaBase,
                    Estado = true,
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return new OperacionAgricolaCreateResult
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                LoteId = lote.LoteId,
                CodigoLote = lote.CodigoLote,
                CostoTotal = costoTotal,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar la cosecha desde operaciones.", nameof(request), ex);
        }
    }

    private async Task CancelHarvestAsync(OperacionAgricola operacion, CancellationToken cancellationToken)
    {
        if (operacion.LoteId is not int loteId || operacion.ProductoId is not int productoId || operacion.Cantidad is not decimal cantidad)
        {
            throw new ArgumentException("La operación de cosecha no tiene la relación completa con lote, producto o cantidad.", nameof(operacion));
        }

        var lote = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == loteId, cancellationToken);

        if (lote is null)
        {
            throw new ArgumentException("El lote asociado no existe.", nameof(operacion));
        }

        if (lote.CantidadDisponible != cantidad || lote.CantidadInicial != cantidad)
        {
            throw new ArgumentException("No se puede anular porque el lote ya tuvo movimientos posteriores.", nameof(operacion));
        }

        var producto = await dbContext.Productos
            .SingleOrDefaultAsync(x => x.ProductoId == productoId, cancellationToken);

        if (producto is null)
        {
            throw new ArgumentException("El producto asociado no existe.", nameof(operacion));
        }

        if ((producto.Stock ?? 0m) < cantidad)
        {
            throw new ArgumentException("No se puede anular porque el stock actual es menor al cosechado.", nameof(operacion));
        }

        producto.Stock = (producto.Stock ?? 0m) - cantidad;
        lote.CantidadDisponible = 0m;
        lote.Estado = "Inactivo";

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = lote.LoteId,
            Tipo = "AnulacionCosecha",
            VentaId = null,
            Cantidad = cantidad,
            Observacion = $"Anulación de cosecha operación #{operacion.OperacionAgricolaId}",
        });
    }

    private static bool IsHarvestOperation(string? tipoOperacion)
    {
        return string.Equals(tipoOperacion, TiposOperacionAgricola.Cosecha, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> AutoGenerateHarvestLotCodeAsync(string cultivo, int parcelaId, int productoId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"COS-{BuildCultivoPrefix(cultivo)}-{parcelaId:000}-{productoId:000}-{fecha:yyyyMMdd}-";
        var correlativo = await dbContext.Lotes
            .AsNoTracking()
            .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);

        return $"{prefix}{correlativo + 1:000}";
    }

    private static string? NormalizeCultivo(string? cultivo)
    {
        var normalized = (cultivo ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string BuildCultivoPrefix(string cultivo)
    {
        var compact = new string(cultivo
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

    private static string? NormalizeText(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
