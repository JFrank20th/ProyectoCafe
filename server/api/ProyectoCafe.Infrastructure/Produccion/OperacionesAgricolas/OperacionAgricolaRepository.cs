using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Compras.RecepcionesCafe;
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
            join cultivo in dbContext.Cultivos.AsNoTracking() on operacion.CultivoId equals cultivo.CultivoId
            join finca in dbContext.Fincas.AsNoTracking() on cultivo.FincaId equals finca.FincaId
            join insumoRef in dbContext.Insumos.AsNoTracking() on operacion.InsumoId equals insumoRef.InsumoId into insumos
            from insumo in insumos.DefaultIfEmpty()
            join productoRef in dbContext.Productos.AsNoTracking() on operacion.ProductoId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            join loteRef in dbContext.Lotes.AsNoTracking() on operacion.LoteId equals loteRef.LoteId into lotes
            from lote in lotes.DefaultIfEmpty()
            join recepcionRef in dbContext.RecepcionesCafe.AsNoTracking() on operacion.RecepcionCafeId equals recepcionRef.RecepcionCafeId into recepciones
            from recepcion in recepciones.DefaultIfEmpty()
            orderby operacion.FechaOperacion descending, operacion.OperacionAgricolaId descending
            select new OperacionAgricolaResponse
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                CultivoId = operacion.CultivoId,
                CultivoNombre = cultivo.Nombre,
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
                CodigoLote = lote != null ? lote.CodigoLote : recepcion != null ? recepcion.CodigoLote : null,
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
            join cultivo in dbContext.Cultivos.AsNoTracking() on operacion.CultivoId equals cultivo.CultivoId
            join finca in dbContext.Fincas.AsNoTracking() on cultivo.FincaId equals finca.FincaId
            join insumoRef in dbContext.Insumos.AsNoTracking() on operacion.InsumoId equals insumoRef.InsumoId into insumos
            from insumo in insumos.DefaultIfEmpty()
            join productoRef in dbContext.Productos.AsNoTracking() on operacion.ProductoId equals productoRef.ProductoId into productos
            from producto in productos.DefaultIfEmpty()
            join loteRef in dbContext.Lotes.AsNoTracking() on operacion.LoteId equals loteRef.LoteId into lotes
            from lote in lotes.DefaultIfEmpty()
            join recepcionRef in dbContext.RecepcionesCafe.AsNoTracking() on operacion.RecepcionCafeId equals recepcionRef.RecepcionCafeId into recepciones
            from recepcion in recepciones.DefaultIfEmpty()
            where operacion.OperacionAgricolaId == id
            select new OperacionAgricolaResponse
            {
                OperacionAgricolaId = operacion.OperacionAgricolaId,
                CultivoId = operacion.CultivoId,
                CultivoNombre = cultivo.Nombre,
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
                CodigoLote = lote != null ? lote.CodigoLote : recepcion != null ? recepcion.CodigoLote : null,
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
        if (request.CultivoId <= 0)
        {
            throw new ArgumentException("CultivoId inválido.", nameof(request));
        }

        var tipoOperacion = TiposOperacionAgricola.Normalize(request.TipoOperacion);
        if (tipoOperacion is null)
        {
            throw new ArgumentException("TipoOperacion inválido.", nameof(request));
        }

        var esCosecha = TiposOperacionAgricola.IsHarvest(tipoOperacion);
        var usaInsumo = TiposOperacionAgricola.UsesSupply(tipoOperacion);

        if (!request.FechaOperacion.HasValue)
        {
            throw new ArgumentException("FechaOperacion es requerida.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Observaciones))
        {
            throw new ArgumentException("Observaciones es requerida.", nameof(request));
        }

        if (request.CostoManoObra < 0 || request.CostoOtros < 0)
        {
            throw new ArgumentException("Los costos no pueden ser negativos.", nameof(request));
        }

        if (request.CantidadInsumo.HasValue && request.CantidadInsumo.Value < 0)
        {
            throw new ArgumentException("CantidadInsumo no puede ser negativa.", nameof(request));
        }

        var cultivo = await dbContext.Cultivos
            .SingleOrDefaultAsync(x => x.CultivoId == request.CultivoId, cancellationToken);

        if (cultivo is null || cultivo.Estado == false)
        {
            throw new ArgumentException("CultivoId inválido o inactivo.", nameof(request));
        }

        var finca = await dbContext.Fincas
            .SingleOrDefaultAsync(x => x.FincaId == cultivo.FincaId, cancellationToken);

        if (finca is null || finca.Estado == false)
        {
            throw new ArgumentException("La finca asociada al cultivo no existe o está inactiva.", nameof(request));
        }

        if (esCosecha)
        {
            return await CreateHarvestAsync(request, tipoOperacion, cultivo, cancellationToken);
        }

        if (usaInsumo)
        {
            if (!request.InsumoId.HasValue || !request.CantidadInsumo.HasValue || request.CantidadInsumo.Value <= 0)
            {
                throw new ArgumentException("Debes indicar un insumo y una cantidad mayor a 0 para este tipo de operación.", nameof(request));
            }
        }
        else if (request.InsumoId.HasValue || request.CantidadInsumo.HasValue)
        {
            throw new ArgumentException("Este tipo de operación no admite insumos asociados.", nameof(request));
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
                CultivoId = request.CultivoId,
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
                    Nombre = $"Costo operación {tipoOperacion} - cultivo {cultivo.Nombre}",
                    TipoCosto = TiposCostoContable.OperacionAgricola,
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = operacion.OperacionAgricolaId,
                    CultivoId = operacion.CultivoId,
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

    public async Task<bool> UpdateAsync(int id, OperacionAgricolaCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.CultivoId <= 0)
        {
            throw new ArgumentException("CultivoId inválido.", nameof(request));
        }

        var tipoOperacion = TiposOperacionAgricola.Normalize(request.TipoOperacion);
        if (tipoOperacion is null)
        {
            throw new ArgumentException("TipoOperacion inválido.", nameof(request));
        }

        if (!request.FechaOperacion.HasValue)
        {
            throw new ArgumentException("FechaOperacion es requerida.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Observaciones))
        {
            throw new ArgumentException("Observaciones es requerida.", nameof(request));
        }

        if (request.CostoManoObra < 0 || request.CostoOtros < 0)
        {
            throw new ArgumentException("Los costos no pueden ser negativos.", nameof(request));
        }

        var operacion = await dbContext.OperacionesAgricolas
            .SingleOrDefaultAsync(x => x.OperacionAgricolaId == id, cancellationToken);

        if (operacion is null)
        {
            return false;
        }

        if (operacion.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("No se puede editar una operación anulada.", nameof(id));
        }

        if (!string.Equals(operacion.TipoOperacion, tipoOperacion, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("No se puede cambiar el tipo de operación en una edición.", nameof(request));
        }

        if (operacion.CultivoId != request.CultivoId)
        {
            throw new ArgumentException("No se puede cambiar el cultivo en una edición.", nameof(request));
        }

        var esCosecha = TiposOperacionAgricola.IsHarvest(tipoOperacion);
        var usaInsumo = TiposOperacionAgricola.UsesSupply(tipoOperacion);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (esCosecha)
            {
                if (request.Cantidad != operacion.Cantidad)
                {
                    throw new ArgumentException("No se puede cambiar la cantidad de una cosecha registrada.", nameof(request));
                }

                if (request.InsumoId.HasValue || request.CantidadInsumo.HasValue)
                {
                    throw new ArgumentException("La cosecha no admite insumos asociados.", nameof(request));
                }

                if (request.CostoManoObra != 0)
                {
                    throw new ArgumentException("La cosecha no admite CostoManoObra.", nameof(request));
                }

                operacion.FechaOperacion = request.FechaOperacion;
                operacion.CostoManoObra = 0m;
                operacion.CostoOtros = request.CostoOtros;
                operacion.CostoTotal = Math.Round(request.CostoOtros, 2, MidpointRounding.AwayFromZero);
                operacion.Observaciones = NormalizeText(request.Observaciones);
            }
            else
            {
                decimal costoInsumo = 0m;
                decimal? cantidadInsumo = null;

                if (operacion.InsumoId.HasValue && operacion.CantidadInsumo.HasValue && operacion.CantidadInsumo.Value > 0)
                {
                    var insumoAnterior = await dbContext.Insumos
                        .SingleOrDefaultAsync(x => x.InsumoId == operacion.InsumoId.Value, cancellationToken);

                    if (insumoAnterior is null)
                    {
                        throw new ArgumentException("El insumo asociado no existe.", nameof(id));
                    }

                    insumoAnterior.Cantidad = (insumoAnterior.Cantidad ?? 0m) + operacion.CantidadInsumo.Value;
                }

                if (usaInsumo)
                {
                    if (!request.InsumoId.HasValue || !request.CantidadInsumo.HasValue || request.CantidadInsumo.Value <= 0)
                    {
                        throw new ArgumentException("Debes indicar un insumo y una cantidad mayor a 0 para este tipo de operación.", nameof(request));
                    }

                    var insumo = await dbContext.Insumos
                        .SingleOrDefaultAsync(x => x.InsumoId == request.InsumoId.Value, cancellationToken);

                    if (insumo is null || insumo.Estado == false)
                    {
                        throw new ArgumentException("InsumoId inválido o inactivo.", nameof(request));
                    }

                    var disponible = insumo.Cantidad ?? 0m;
                    if (disponible < request.CantidadInsumo.Value)
                    {
                        throw new ArgumentException("La cantidad del insumo es insuficiente para actualizar la operación.", nameof(request));
                    }

                    cantidadInsumo = request.CantidadInsumo.Value;
                    insumo.Cantidad = disponible - cantidadInsumo.Value;
                    costoInsumo = Math.Round(cantidadInsumo.Value * (insumo.CostoUnitario ?? 0m), 2, MidpointRounding.AwayFromZero);
                    operacion.InsumoId = insumo.InsumoId;
                    operacion.CantidadInsumo = cantidadInsumo;
                }
                else
                {
                    if (request.InsumoId.HasValue || request.CantidadInsumo.HasValue)
                    {
                        throw new ArgumentException("Este tipo de operación no admite insumos asociados.", nameof(request));
                    }

                    operacion.InsumoId = null;
                    operacion.CantidadInsumo = null;
                }

                operacion.FechaOperacion = request.FechaOperacion;
                operacion.CostoInsumo = costoInsumo;
                operacion.CostoManoObra = request.CostoManoObra;
                operacion.CostoOtros = request.CostoOtros;
                operacion.CostoTotal = Math.Round(costoInsumo + request.CostoManoObra + request.CostoOtros, 2, MidpointRounding.AwayFromZero);
                operacion.Observaciones = NormalizeText(request.Observaciones);
            }

            await SyncOperationCostsAsync(operacion, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo actualizar la operación agrícola.", nameof(request), ex);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
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

        if (TiposOperacionAgricola.IsHarvest(operacion.TipoOperacion))
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
        Cultivo cultivo,
        CancellationToken cancellationToken)
    {
        if (request.Cantidad is not decimal cantidad || cantidad <= 0)
        {
            throw new ArgumentException("Cantidad debe ser mayor a 0.", nameof(request));
        }

        if (request.InsumoId.HasValue || request.CantidadInsumo.HasValue)
        {
            throw new ArgumentException("La cosecha no admite insumos asociados.", nameof(request));
        }

        var configuracion = await ResolveHarvestConfigurationAsync(request, cultivo, cancellationToken);
        var tipoCosecha = NormalizeCultivo(configuracion.NombreTipoCosecha)
            ?? "Cosecha";
        var fechaBase = (request.FechaOperacion ?? DateTime.Today).Date;
        var costoTotal = Math.Round(request.CostoManoObra + request.CostoOtros, 2, MidpointRounding.AwayFromZero);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var operacion = new OperacionAgricola
            {
                CultivoId = request.CultivoId,
                TipoOperacion = tipoOperacion,
                FechaOperacion = request.FechaOperacion,
                InsumoId = null,
                ProductoId = null,
                CantidadInsumo = null,
                Cantidad = cantidad,
                Cultivo = tipoCosecha,
                Humedad = request.Humedad,
                LoteId = null,
                RecepcionCafeId = null,
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
                    Nombre = $"Costo cosecha {tipoCosecha} - cultivo {cultivo.Nombre}",
                    TipoCosto = TiposCostoContable.CosechaCafe,
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = operacion.OperacionAgricolaId,
                    CultivoId = operacion.CultivoId,
                    LoteId = null,
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
                LoteId = null,
                CodigoLote = null,
                CostoTotal = costoTotal,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar la cosecha desde operaciones.", nameof(request), ex);
        }
    }

    private async Task<CultivoConfiguracionCosecha> ResolveHarvestConfigurationAsync(
        OperacionAgricolaCreateRequest request,
        Cultivo cultivo,
        CancellationToken cancellationToken)
    {
        if (request.CultivoConfiguracionCosechaId is not int configuracionId)
        {
            throw new ArgumentException("Debes seleccionar un tipo de cosecha configurado para el cultivo.", nameof(request));
        }

        var configuracion = await dbContext.CultivosConfiguracionesCosecha
            .AsNoTracking()
            .Where(x => x.CultivoConfiguracionCosechaId == configuracionId && x.CultivoId == cultivo.CultivoId)
            .SingleOrDefaultAsync(cancellationToken);

        if (configuracion is null)
        {
            throw new ArgumentException("La configuración de cosecha seleccionada no pertenece al cultivo.", nameof(request));
        }

        return configuracion;
    }

    private async Task CancelHarvestAsync(OperacionAgricola operacion, CancellationToken cancellationToken)
    {
        if (operacion.RecepcionCafeId is not int recepcionCafeId)
        {
            return;
        }

        if (operacion.Cantidad is not decimal cantidad)
        {
            throw new ArgumentException("La operación de cosecha no tiene la cantidad registrada.", nameof(operacion));
        }

        var recepcion = await dbContext.RecepcionesCafe
            .SingleOrDefaultAsync(x => x.RecepcionCafeId == recepcionCafeId, cancellationToken);

        if (recepcion is null)
        {
            throw new ArgumentException("La recepción asociada no existe.", nameof(operacion));
        }

        if (recepcion.CantidadDisponible != cantidad)
        {
            throw new ArgumentException("No se puede anular porque la recepción derivada ya tuvo procesos posteriores.", nameof(operacion));
        }

        recepcion.Estado = "Anulada";
        recepcion.CantidadDisponible = 0m;
    }

    private async Task SyncOperationCostsAsync(OperacionAgricola operacion, CancellationToken cancellationToken)
    {
        var costoFecha = operacion.FechaOperacion ?? DateTime.Now;
        var esCosecha = TiposOperacionAgricola.IsHarvest(operacion.TipoOperacion);

        var costoExistente = await dbContext.Costos
            .Where(x => x.FuenteModulo == "Produccion.OperacionesAgricolas" && x.ReferenciaId == operacion.OperacionAgricolaId)
            .SingleOrDefaultAsync(cancellationToken);

        var costos = costoExistente is null
            ? new List<Costo>()
            : new List<Costo> { costoExistente };

        if (operacion.CostoTotal <= 0)
        {
            foreach (var costo in costos)
            {
                costo.Estado = false;
            }

            return;
        }

        if (costos.Count == 0)
        {
            dbContext.Costos.Add(new Costo
            {
                Nombre = await BuildCostNameAsync(operacion, esCosecha, cancellationToken),
                TipoCosto = esCosecha ? TiposCostoContable.CosechaCafe : TiposCostoContable.OperacionAgricola,
                FuenteModulo = "Produccion.OperacionesAgricolas",
                ReferenciaId = operacion.OperacionAgricolaId,
                CultivoId = operacion.CultivoId,
                LoteId = esCosecha ? operacion.LoteId : null,
                Monto = operacion.CostoTotal,
                Fecha = costoFecha,
                Estado = true,
            });

            return;
        }

        foreach (var costo in costos)
        {
            costo.Nombre ??= await BuildCostNameAsync(operacion, esCosecha, cancellationToken);
            costo.TipoCosto = esCosecha ? TiposCostoContable.CosechaCafe : TiposCostoContable.OperacionAgricola;
            costo.CultivoId = operacion.CultivoId;
            costo.LoteId = esCosecha ? operacion.LoteId : null;
            costo.Monto = operacion.CostoTotal;
            costo.Fecha = costoFecha;
            costo.Estado = true;
        }
    }

    private async Task<string> BuildCostNameAsync(OperacionAgricola operacion, bool esCosecha, CancellationToken cancellationToken)
    {
        if (esCosecha)
        {
            var codigoLote = operacion.RecepcionCafeId.HasValue
                ? await dbContext.RecepcionesCafe
                    .AsNoTracking()
                    .Where(x => x.RecepcionCafeId == operacion.RecepcionCafeId.Value)
                    .Select(x => x.CodigoLote)
                    .SingleOrDefaultAsync(cancellationToken)
                : null;

            return $"Costo cosecha {codigoLote ?? $"operación {operacion.OperacionAgricolaId}"}";
        }

        var cultivoNombre = await dbContext.Cultivos
            .AsNoTracking()
            .Where(x => x.CultivoId == operacion.CultivoId)
            .Select(x => x.Nombre)
            .SingleOrDefaultAsync(cancellationToken);

        return $"Costo operación {operacion.TipoOperacion} - cultivo {cultivoNombre ?? operacion.CultivoId.ToString()}";
    }

    private async Task<string> AutoGenerateHarvestLotCodeAsync(string cultivo, int cultivoId, int productoId, DateTime fecha, CancellationToken cancellationToken)
    {
        var prefix = $"COS-{BuildCultivoPrefix(cultivo)}-{cultivoId:000}-{productoId:000}-{fecha:yyyyMMdd}-";
        var correlativo = await dbContext.RecepcionesCafe
            .AsNoTracking()
            .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);

        return $"{prefix}{correlativo + 1:000}";
    }

    private async Task<Proveedor> ResolveHarvestSupplierAsync(Finca finca, Cultivo cultivo, CancellationToken cancellationToken)
    {
        var supplierName = $"{finca.Nombre} / {cultivo.Nombre}";

        var proveedor = await dbContext.Proveedores
            .SingleOrDefaultAsync(x => x.Nombre == supplierName, cancellationToken);

        if (proveedor is not null)
        {
            if (proveedor.EsInterno != true)
            {
                proveedor.EsInterno = true;
            }

            if (proveedor.Estado == false)
            {
                proveedor.Estado = true;
            }

            return proveedor;
        }

        proveedor = new Proveedor
        {
            Nombre = supplierName,
            Direccion = finca.Ubicacion,
            EsInterno = true,
            Estado = true,
        };

        dbContext.Proveedores.Add(proveedor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return proveedor;
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
