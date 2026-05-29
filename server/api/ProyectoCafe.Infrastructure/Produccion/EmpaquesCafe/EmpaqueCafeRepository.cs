using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Produccion.EmpaquesCafe;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Inventario.Lotes;

namespace ProyectoCafe.Infrastructure.Produccion.EmpaquesCafe;

public sealed class EmpaqueCafeRepository(ProyectoCafeDbContext dbContext) : IEmpaqueCafeRepository
{
    public async Task<IReadOnlyList<EmpaqueCafeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var empaques = await (
            from empaque in dbContext.EmpaquesCafe.AsNoTracking()
            join loteOrigen in dbContext.Lotes.AsNoTracking() on empaque.LoteOrigenId equals loteOrigen.LoteId
            join productoOrigen in dbContext.Productos.AsNoTracking() on loteOrigen.ProductoId equals productoOrigen.ProductoId
            orderby empaque.FechaEmpaque descending, empaque.EmpaqueCafeId descending
            select new EmpaqueCafeResponse
            {
                EmpaqueCafeId = empaque.EmpaqueCafeId,
                LoteOrigenId = empaque.LoteOrigenId,
                CodigoLoteOrigen = loteOrigen.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = productoOrigen.UnidadMedida,
                FechaEmpaque = empaque.FechaEmpaque,
                CantidadOrigenConsumida = empaque.CantidadOrigenConsumida,
                CantidadEmpaques = empaque.CantidadEmpaques,
                PesoTotalGramos = empaque.PesoTotalGramos,
                CostoEmpaque = empaque.CostoEmpaque,
                CostoOrigenTotal = 0m,
                CostoTotal = 0m,
                Observaciones = empaque.Observaciones,
                Estado = empaque.Estado,
            })
            .ToListAsync(cancellationToken);

        await AttachDetallesAsync(empaques, cancellationToken);
        return empaques;
    }

    public async Task<EmpaqueCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var empaque = await (
            from entity in dbContext.EmpaquesCafe.AsNoTracking()
            join loteOrigen in dbContext.Lotes.AsNoTracking() on entity.LoteOrigenId equals loteOrigen.LoteId
            join productoOrigen in dbContext.Productos.AsNoTracking() on loteOrigen.ProductoId equals productoOrigen.ProductoId
            where entity.EmpaqueCafeId == id
            select new EmpaqueCafeResponse
            {
                EmpaqueCafeId = entity.EmpaqueCafeId,
                LoteOrigenId = entity.LoteOrigenId,
                CodigoLoteOrigen = loteOrigen.CodigoLote,
                ProductoOrigenId = productoOrigen.ProductoId,
                ProductoOrigenNombre = productoOrigen.Nombre,
                UnidadMedidaOrigen = productoOrigen.UnidadMedida,
                FechaEmpaque = entity.FechaEmpaque,
                CantidadOrigenConsumida = entity.CantidadOrigenConsumida,
                CantidadEmpaques = entity.CantidadEmpaques,
                PesoTotalGramos = entity.PesoTotalGramos,
                CostoEmpaque = entity.CostoEmpaque,
                CostoOrigenTotal = 0m,
                CostoTotal = 0m,
                Observaciones = entity.Observaciones,
                Estado = entity.Estado,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (empaque is null)
        {
            return null;
        }

        await AttachDetallesAsync([empaque], cancellationToken);
        return empaque;
    }

    public async Task<EmpaqueCafeCreateResult> CreateAsync(EmpaqueCafeCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.LoteOrigenId <= 0)
        {
            throw new ArgumentException("LoteOrigenId inválido.", nameof(request));
        }

        if (request.CostoEmpaque < 0)
        {
            throw new ArgumentException("CostoEmpaque no puede ser negativo.", nameof(request));
        }

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("Detalles son requeridos.", nameof(request));
        }

        var origen = await (
            from lote in dbContext.Lotes
            join producto in dbContext.Productos on lote.ProductoId equals producto.ProductoId
            join presentacionRef in dbContext.Presentaciones on producto.PresentacionId equals presentacionRef.PresentacionId into presentacionGroup
            from presentacion in presentacionGroup.DefaultIfEmpty()
            where lote.LoteId == request.LoteOrigenId
            select new
            {
                Lote = lote,
                Producto = producto,
                PresentacionCantidad = presentacion != null ? presentacion.Cantidad : (decimal?)null,
                PresentacionUnidadMedida = presentacion != null ? presentacion.UnidadMedida : null,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (origen is null)
        {
            throw new ArgumentException("LoteOrigenId inválido.", nameof(request));
        }

        if (!origen.Lote.Estado.Equals("Activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El lote origen debe estar activo.", nameof(request));
        }

        if (origen.Producto.Estado == false)
        {
            throw new ArgumentException("El producto origen está inactivo.", nameof(request));
        }

        var productoSalidaIds = request.Detalles.Select(x => x.ProductoSalidaId).Distinct().ToList();
        var presentacionIds = request.Detalles.Select(x => x.PresentacionId).Distinct().ToList();

        if (productoSalidaIds.Any(x => x <= 0) || presentacionIds.Any(x => x <= 0))
        {
            throw new ArgumentException("ProductoSalidaId y PresentacionId deben ser válidos.", nameof(request));
        }

        var productosSalida = await dbContext.Productos
            .Where(x => productoSalidaIds.Contains(x.ProductoId))
            .ToDictionaryAsync(x => x.ProductoId, cancellationToken);

        if (productosSalida.Count != productoSalidaIds.Count)
        {
            throw new ArgumentException("Uno o más ProductoSalidaId no existen.", nameof(request));
        }

        var presentaciones = await dbContext.Presentaciones
            .Where(x => presentacionIds.Contains(x.PresentacionId))
            .ToDictionaryAsync(x => x.PresentacionId, cancellationToken);

        if (presentaciones.Count != presentacionIds.Count)
        {
            throw new ArgumentException("Una o más PresentacionId no existen.", nameof(request));
        }

        var costoOrigenSnapshot = await new LoteCostCalculator(dbContext).CalculateAsync(request.LoteOrigenId, cancellationToken);
        var detallesPlan = new List<DetalleEmpaquePlan>(request.Detalles.Count);

        foreach (var detalle in request.Detalles)
        {
            if (detalle.Cantidad <= 0)
            {
                throw new ArgumentException("Cantidad debe ser mayor a 0 en detalle.", nameof(request));
            }

            var productoSalida = productosSalida[detalle.ProductoSalidaId];
            var presentacion = presentaciones[detalle.PresentacionId];

            if (productoSalida.Estado == false)
            {
                throw new ArgumentException($"ProductoSalidaId {detalle.ProductoSalidaId} está inactivo.", nameof(request));
            }

            if (productoSalida.PresentacionId != detalle.PresentacionId)
            {
                throw new ArgumentException($"ProductoSalidaId {detalle.ProductoSalidaId} no corresponde a la presentación seleccionada.", nameof(request));
            }

            if (!productoSalida.PermiteDecimal && detalle.Cantidad != decimal.Truncate(detalle.Cantidad))
            {
                throw new ArgumentException($"ProductoSalidaId {detalle.ProductoSalidaId} no permite cantidad decimal.", nameof(request));
            }

            var pesoUnitarioGramos = ConvertirPresentacionAGramos(presentacion.Cantidad, presentacion.UnidadMedida);
            var pesoTotalGramos = Math.Round(detalle.Cantidad * pesoUnitarioGramos, 3, MidpointRounding.AwayFromZero);
            detallesPlan.Add(new DetalleEmpaquePlan(detalle, productoSalida, presentacion, pesoUnitarioGramos, pesoTotalGramos));
        }

        var pesoTotal = detallesPlan.Sum(x => x.PesoTotalGramos);
        if (pesoTotal <= 0)
        {
            throw new ArgumentException("El peso total empacado debe ser mayor a 0.", nameof(request));
        }

        var pesoOrigenUnitarioGramos = TryConvertirPresentacionAGramos(origen.PresentacionCantidad, origen.PresentacionUnidadMedida);
        var cantidadOrigenConsumida = ConvertirGramosAUnidadOrigen(pesoTotal, origen.Producto.UnidadMedida, pesoOrigenUnitarioGramos);
        if (origen.Lote.CantidadDisponible < cantidadOrigenConsumida)
        {
            throw new ArgumentException("El lote origen no tiene cantidad suficiente para el empaque solicitado.", nameof(request));
        }

        var detallesAsignados = AsignarCostos(detallesPlan, cantidadOrigenConsumida, request.CostoEmpaque, costoOrigenSnapshot.CostoUnitario);
        var fechaBase = (request.FechaEmpaque ?? DateTime.Today).Date;
        var correlativos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            origen.Lote.CantidadDisponible -= cantidadOrigenConsumida;
            origen.Producto.Stock = (origen.Producto.Stock ?? 0m) - cantidadOrigenConsumida;

            var empaque = new EmpaqueCafe
            {
                LoteOrigenId = request.LoteOrigenId,
                FechaEmpaque = request.FechaEmpaque,
                CantidadOrigenConsumida = cantidadOrigenConsumida,
                CantidadEmpaques = detallesAsignados.Sum(x => x.Detalle.Cantidad),
                PesoTotalGramos = pesoTotal,
                CostoEmpaque = request.CostoEmpaque,
                Observaciones = NormalizarTexto(request.Observaciones),
                Estado = "Registrado",
            };

            dbContext.EmpaquesCafe.Add(empaque);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = origen.Lote.LoteId,
                Tipo = "EmpaqueSalida",
                VentaId = null,
                Cantidad = cantidadOrigenConsumida,
                Observacion = $"Salida por empaque #{empaque.EmpaqueCafeId}",
            });

            foreach (var detalle in detallesAsignados)
            {
                var codigoLoteSalida = NormalizarCodigo(detalle.Detalle.CodigoLoteSalida)
                    ?? await AutoGenerarCodigoSalidaAsync(detalle.Presentacion.Codigo, detalle.ProductoSalida.ProductoId, fechaBase, correlativos, cancellationToken);

                detalle.LoteSalida = new Lote
                {
                    ProductoId = detalle.ProductoSalida.ProductoId,
                    CodigoLote = codigoLoteSalida,
                    CantidadInicial = detalle.Detalle.Cantidad,
                    CantidadDisponible = detalle.Detalle.Cantidad,
                    Estado = "Activo",
                };

                dbContext.Lotes.Add(detalle.LoteSalida);
                detalle.ProductoSalida.ManejaLotes = true;
                detalle.ProductoSalida.Stock = (detalle.ProductoSalida.Stock ?? 0m) + detalle.Detalle.Cantidad;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var createDetails = new List<EmpaqueCafeCreateDetailResult>(detallesAsignados.Count);
            foreach (var detalle in detallesAsignados)
            {
                dbContext.EmpaquesCafeDetalles.Add(new EmpaqueCafeDetalle
                {
                    EmpaqueCafeId = empaque.EmpaqueCafeId,
                    ProductoSalidaId = detalle.ProductoSalida.ProductoId,
                    PresentacionId = detalle.Presentacion.PresentacionId,
                    LoteSalidaId = detalle.LoteSalida!.LoteId,
                    Cantidad = detalle.Detalle.Cantidad,
                    PesoGramosUnitario = detalle.PesoUnitarioGramos,
                    PesoTotalGramos = detalle.PesoTotalGramos,
                    CantidadOrigenAsignada = detalle.CantidadOrigenAsignada,
                    CostoOrigenAsignado = detalle.CostoOrigenAsignado,
                    CostoEmpaqueAsignado = detalle.CostoEmpaqueAsignado,
                    Estado = "Registrado",
                });

                dbContext.LoteMovimientos.Add(new LoteMovimiento
                {
                    LoteId = detalle.LoteSalida!.LoteId,
                    Tipo = "EmpaqueEntrada",
                    VentaId = null,
                    Cantidad = detalle.Detalle.Cantidad,
                    Observacion = $"Ingreso por empaque #{empaque.EmpaqueCafeId}",
                });

                if (detalle.CostoEmpaqueAsignado > 0)
                {
                    dbContext.Costos.Add(new Costo
                    {
                        Nombre = $"Costo empaque {detalle.LoteSalida.CodigoLote}",
                        TipoCosto = TiposCostoContable.EmpaqueCafe,
                        FuenteModulo = "Produccion.EmpaquesCafe",
                        ReferenciaId = empaque.EmpaqueCafeId,
                        LoteId = detalle.LoteSalida.LoteId,
                        Monto = detalle.CostoEmpaqueAsignado,
                        Fecha = request.FechaEmpaque ?? fechaBase,
                        Estado = true,
                    });
                }

                createDetails.Add(new EmpaqueCafeCreateDetailResult
                {
                    ProductoSalidaId = detalle.ProductoSalida.ProductoId,
                    LoteSalidaId = detalle.LoteSalida.LoteId,
                    CodigoLoteSalida = detalle.LoteSalida.CodigoLote,
                    Cantidad = detalle.Detalle.Cantidad,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new EmpaqueCafeCreateResult
            {
                EmpaqueCafeId = empaque.EmpaqueCafeId,
                Detalles = createDetails,
            };
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ArgumentException("No se pudo registrar el empaque. Verifica que los códigos de lote de salida no estén duplicados.", nameof(request), ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var empaque = await dbContext.EmpaquesCafe
            .SingleOrDefaultAsync(x => x.EmpaqueCafeId == id, cancellationToken);

        if (empaque is null)
        {
            return false;
        }

        if (empaque.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var detalles = await dbContext.EmpaquesCafeDetalles
            .Where(x => x.EmpaqueCafeId == id)
            .OrderBy(x => x.EmpaqueCafeDetalleId)
            .ToListAsync(cancellationToken);

        if (detalles.Count == 0)
        {
            throw new ArgumentException("El empaque no tiene detalles para revertir.", nameof(id));
        }

        var loteOrigen = await dbContext.Lotes
            .SingleOrDefaultAsync(x => x.LoteId == empaque.LoteOrigenId, cancellationToken);
        var productoOrigen = loteOrigen is null
            ? null
            : await dbContext.Productos.SingleOrDefaultAsync(x => x.ProductoId == loteOrigen.ProductoId, cancellationToken);

        if (loteOrigen is null || productoOrigen is null)
        {
            throw new ArgumentException("Las relaciones del empaque están incompletas y no puede anularse.", nameof(id));
        }

        var loteIds = detalles.Select(x => x.LoteSalidaId).Distinct().ToList();
        var productoIds = detalles.Select(x => x.ProductoSalidaId).Distinct().ToList();

        var lotesSalida = await dbContext.Lotes
            .Where(x => loteIds.Contains(x.LoteId))
            .ToDictionaryAsync(x => x.LoteId, cancellationToken);

        var productosSalida = await dbContext.Productos
            .Where(x => productoIds.Contains(x.ProductoId))
            .ToDictionaryAsync(x => x.ProductoId, cancellationToken);

        foreach (var detalle in detalles)
        {
            if (!lotesSalida.TryGetValue(detalle.LoteSalidaId, out var loteSalida) ||
                !productosSalida.TryGetValue(detalle.ProductoSalidaId, out var productoSalida))
            {
                throw new ArgumentException("Las relaciones de salida del empaque están incompletas y no puede anularse.", nameof(id));
            }

            if (loteSalida.CantidadInicial != detalle.Cantidad || loteSalida.CantidadDisponible != detalle.Cantidad)
            {
                throw new ArgumentException("No se puede anular porque uno de los lotes salida ya tuvo movimientos posteriores.", nameof(id));
            }

            if ((productoSalida.Stock ?? 0m) < detalle.Cantidad)
            {
                throw new ArgumentException("No se puede anular porque el stock actual de un producto salida es insuficiente.", nameof(id));
            }
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        loteOrigen.CantidadDisponible += empaque.CantidadOrigenConsumida;
        productoOrigen.Stock = (productoOrigen.Stock ?? 0m) + empaque.CantidadOrigenConsumida;

        dbContext.LoteMovimientos.Add(new LoteMovimiento
        {
            LoteId = loteOrigen.LoteId,
            Tipo = "AnulacionEmpRet",
            VentaId = null,
            Cantidad = empaque.CantidadOrigenConsumida,
            Observacion = $"Anulación empaque #{empaque.EmpaqueCafeId}",
        });

        foreach (var detalle in detalles)
        {
            var loteSalida = lotesSalida[detalle.LoteSalidaId];
            var productoSalida = productosSalida[detalle.ProductoSalidaId];

            loteSalida.CantidadDisponible = 0m;
            loteSalida.Estado = "Inactivo";
            productoSalida.Stock = (productoSalida.Stock ?? 0m) - detalle.Cantidad;
            detalle.Estado = "Anulada";

            dbContext.LoteMovimientos.Add(new LoteMovimiento
            {
                LoteId = loteSalida.LoteId,
                Tipo = "AnulacionEmpSal",
                VentaId = null,
                Cantidad = detalle.Cantidad,
                Observacion = $"Anulación empaque #{empaque.EmpaqueCafeId}",
            });
        }

        empaque.Estado = "Anulada";

        var costos = await dbContext.Costos
            .Where(x => x.FuenteModulo == "Produccion.EmpaquesCafe" && x.ReferenciaId == empaque.EmpaqueCafeId && x.Estado != false)
            .ToListAsync(cancellationToken);

        foreach (var costo in costos)
        {
            costo.Estado = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task AttachDetallesAsync(IReadOnlyList<EmpaqueCafeResponse> empaques, CancellationToken cancellationToken)
    {
        if (empaques.Count == 0)
        {
            return;
        }

        var empaqueIds = empaques.Select(x => x.EmpaqueCafeId).ToList();
        var detalles = await (
            from detalle in dbContext.EmpaquesCafeDetalles.AsNoTracking()
            join producto in dbContext.Productos.AsNoTracking() on detalle.ProductoSalidaId equals producto.ProductoId
            join presentacion in dbContext.Presentaciones.AsNoTracking() on detalle.PresentacionId equals presentacion.PresentacionId
            join loteSalida in dbContext.Lotes.AsNoTracking() on detalle.LoteSalidaId equals loteSalida.LoteId
            where empaqueIds.Contains(detalle.EmpaqueCafeId)
            orderby detalle.EmpaqueCafeId, detalle.EmpaqueCafeDetalleId
            select new
            {
                detalle.EmpaqueCafeId,
                Response = new EmpaqueCafeDetalleResponse
                {
                    EmpaqueCafeDetalleId = detalle.EmpaqueCafeDetalleId,
                    ProductoSalidaId = detalle.ProductoSalidaId,
                    ProductoSalidaNombre = producto.Nombre,
                    UnidadMedidaSalida = producto.UnidadMedida,
                    PresentacionId = detalle.PresentacionId,
                    PresentacionCodigo = presentacion.Codigo,
                    PresentacionNombre = presentacion.Nombre,
                    PresentacionCantidad = presentacion.Cantidad,
                    PresentacionUnidadMedida = presentacion.UnidadMedida,
                    LoteSalidaId = detalle.LoteSalidaId,
                    CodigoLoteSalida = loteSalida.CodigoLote,
                    Cantidad = detalle.Cantidad,
                    PesoGramosUnitario = detalle.PesoGramosUnitario,
                    PesoTotalGramos = detalle.PesoTotalGramos,
                    CantidadOrigenAsignada = detalle.CantidadOrigenAsignada,
                    CostoOrigenAsignado = detalle.CostoOrigenAsignado,
                    CostoEmpaqueAsignado = detalle.CostoEmpaqueAsignado,
                    CostoTotalAsignado = Math.Round(detalle.CostoOrigenAsignado + detalle.CostoEmpaqueAsignado, 4, MidpointRounding.AwayFromZero),
                    CostoUnitarioSalida = detalle.Cantidad > 0
                        ? Math.Round((detalle.CostoOrigenAsignado + detalle.CostoEmpaqueAsignado) / detalle.Cantidad, 4, MidpointRounding.AwayFromZero)
                        : 0m,
                    Estado = detalle.Estado,
                },
            })
            .ToListAsync(cancellationToken);

        var detallesPorEmpaque = detalles
            .GroupBy(x => x.EmpaqueCafeId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<EmpaqueCafeDetalleResponse>)x.Select(y => y.Response).ToList());

        foreach (var empaque in empaques)
        {
            empaque.Detalles = detallesPorEmpaque.TryGetValue(empaque.EmpaqueCafeId, out var list)
                ? list
                : [];
            empaque.CostoOrigenTotal = Math.Round(empaque.Detalles.Sum(x => x.CostoOrigenAsignado), 4, MidpointRounding.AwayFromZero);
            empaque.CostoTotal = Math.Round(empaque.CostoOrigenTotal + empaque.CostoEmpaque, 4, MidpointRounding.AwayFromZero);
        }
    }

    private async Task<string> AutoGenerarCodigoSalidaAsync(
        string presentacionCodigo,
        int productoSalidaId,
        DateTime fecha,
        Dictionary<string, int> correlativos,
        CancellationToken cancellationToken)
    {
        var prefix = $"EMP-{presentacionCodigo.Trim().ToUpperInvariant()}-{productoSalidaId:000}-{fecha:yyyyMMdd}-";

        if (!correlativos.TryGetValue(prefix, out var correlativoActual))
        {
            correlativoActual = await dbContext.Lotes
                .AsNoTracking()
                .CountAsync(x => x.CodigoLote.StartsWith(prefix), cancellationToken);
        }

        correlativoActual++;
        correlativos[prefix] = correlativoActual;
        return $"{prefix}{correlativoActual:000}";
    }

    private static decimal ConvertirGramosAUnidadOrigen(decimal gramos, string unidadMedida, decimal? pesoPresentacionGramos)
    {
        var unidadNormalizada = (unidadMedida ?? string.Empty).Trim().ToUpperInvariant();

        if (unidadNormalizada == "KG")
        {
            return Math.Round(gramos / 1000m, 3, MidpointRounding.AwayFromZero);
        }

        if (unidadNormalizada == "G")
        {
            return Math.Round(gramos, 3, MidpointRounding.AwayFromZero);
        }

        if (unidadNormalizada == "LB")
        {
            return Math.Round(gramos / 453.59237m, 3, MidpointRounding.AwayFromZero);
        }

        if (unidadNormalizada == "UN" && pesoPresentacionGramos.HasValue && pesoPresentacionGramos.Value > 0)
        {
            return Math.Round(gramos / pesoPresentacionGramos.Value, 3, MidpointRounding.AwayFromZero);
        }

        throw new ArgumentException($"No se puede convertir gramos a la unidad origen '{unidadMedida}'. Usa KG, G, LB o un producto con presentación configurada.");
    }

    private static decimal ConvertirPresentacionAGramos(decimal cantidad, string? unidadMedida)
    {
        var unidadNormalizada = (unidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        if (cantidad <= 0)
        {
            throw new ArgumentException("La presentación seleccionada debe tener una cantidad mayor a 0.");
        }

        return unidadNormalizada switch
        {
            "G" => Math.Round(cantidad, 3, MidpointRounding.AwayFromZero),
            "KG" => Math.Round(cantidad * 1000m, 3, MidpointRounding.AwayFromZero),
            "LB" => Math.Round(cantidad * 453.59237m, 3, MidpointRounding.AwayFromZero),
            _ => throw new ArgumentException($"La presentación usa la unidad '{unidadMedida}' y EmpaquesCafe solo soporta presentaciones ponderales en G, KG o LB."),
        };
    }

    private static decimal? TryConvertirPresentacionAGramos(decimal? cantidad, string? unidadMedida)
    {
        if (!cantidad.HasValue || string.IsNullOrWhiteSpace(unidadMedida))
        {
            return null;
        }

        var unidadNormalizada = unidadMedida.Trim().ToUpperInvariant();
        return unidadNormalizada switch
        {
            "G" => Math.Round(cantidad.Value, 3, MidpointRounding.AwayFromZero),
            "KG" => Math.Round(cantidad.Value * 1000m, 3, MidpointRounding.AwayFromZero),
            "LB" => Math.Round(cantidad.Value * 453.59237m, 3, MidpointRounding.AwayFromZero),
            _ => null,
        };
    }

    private static List<DetalleEmpaqueAsignado> AsignarCostos(
        IReadOnlyList<DetalleEmpaquePlan> detalles,
        decimal cantidadOrigenConsumida,
        decimal costoEmpaque,
        decimal costoOrigenUnitario)
    {
        var pesoTotal = detalles.Sum(x => x.PesoTotalGramos);
        var asignados = new List<DetalleEmpaqueAsignado>(detalles.Count);
        var cantidadOrigenRestante = cantidadOrigenConsumida;
        var costoEmpaqueRestante = costoEmpaque;

        for (var i = 0; i < detalles.Count; i++)
        {
            var detalle = detalles[i];
            var esUltimo = i == detalles.Count - 1;

            var cantidadOrigenAsignada = esUltimo
                ? cantidadOrigenRestante
                : Math.Round(cantidadOrigenConsumida * (detalle.PesoTotalGramos / pesoTotal), 3, MidpointRounding.AwayFromZero);

            var costoEmpaqueAsignado = esUltimo
                ? costoEmpaqueRestante
                : Math.Round(costoEmpaque * (detalle.PesoTotalGramos / pesoTotal), 2, MidpointRounding.AwayFromZero);

            var costoOrigenAsignado = Math.Round(costoOrigenUnitario * cantidadOrigenAsignada, 4, MidpointRounding.AwayFromZero);

            asignados.Add(new DetalleEmpaqueAsignado(
                detalle.Detalle,
                detalle.ProductoSalida,
                detalle.Presentacion,
                detalle.PesoUnitarioGramos,
                detalle.PesoTotalGramos,
                cantidadOrigenAsignada,
                costoOrigenAsignado,
                costoEmpaqueAsignado));

            cantidadOrigenRestante -= cantidadOrigenAsignada;
            costoEmpaqueRestante -= costoEmpaqueAsignado;
        }

        return asignados;
    }

    private static string? NormalizarCodigo(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizarTexto(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record DetalleEmpaquePlan(
        EmpaqueCafeDetalleCreateRequest Detalle,
        Producto ProductoSalida,
        Presentacion Presentacion,
        decimal PesoUnitarioGramos,
        decimal PesoTotalGramos);

    private sealed class DetalleEmpaqueAsignado(
        EmpaqueCafeDetalleCreateRequest detalle,
        Producto productoSalida,
        Presentacion presentacion,
        decimal pesoUnitarioGramos,
        decimal pesoTotalGramos,
        decimal cantidadOrigenAsignada,
        decimal costoOrigenAsignado,
        decimal costoEmpaqueAsignado)
    {
        public EmpaqueCafeDetalleCreateRequest Detalle { get; } = detalle;

        public Producto ProductoSalida { get; } = productoSalida;

        public Presentacion Presentacion { get; } = presentacion;

    public decimal PesoUnitarioGramos { get; } = pesoUnitarioGramos;

        public decimal PesoTotalGramos { get; } = pesoTotalGramos;

        public decimal CantidadOrigenAsignada { get; } = cantidadOrigenAsignada;

        public decimal CostoOrigenAsignado { get; } = costoOrigenAsignado;

        public decimal CostoEmpaqueAsignado { get; } = costoEmpaqueAsignado;

        public Lote? LoteSalida { get; set; }
    }
}
