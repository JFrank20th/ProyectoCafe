using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/dashboard");
        Map(endpoints, "/api/Dashboard"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Dashboard");

        group.MapGet("/summary", async (
            string? period,
            string? from,
            string? to,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            DashboardPeriodWindow window;
            var nowUtc = DateTime.UtcNow;

            try
            {
                window = DashboardPeriodWindow.Resolve(period, from, to, nowUtc);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(period)] = [ex.Message],
                });
            }

            var current = await BuildMetricsAsync(db, window, ct);

            DashboardPreviousPeriodSummary? previousPeriod = null;
            var previousWindow = window.CreatePreviousWindow();
            if (previousWindow is not null)
            {
                var previous = await BuildMetricsAsync(db, previousWindow, ct);
                previousPeriod = new DashboardPreviousPeriodSummary
                {
                    Window = previousWindow.ToResponse(),
                    Production = previous.Production,
                    Commercial = previous.Commercial,
                    Finance = previous.Finance,
                    Traceability = previous.Traceability,
                };
            }

            var response = new DashboardSummaryResponse
            {
                Window = window.ToResponse(),
                PreviousPeriod = previousPeriod,
                Production = current.Production,
                Commercial = current.Commercial,
                Finance = current.Finance,
                Traceability = current.Traceability,
                GeneratedAtUtc = nowUtc,
            };

            return Results.Ok(response);
        })
        .RequireAuthorization();
    }

    private static async Task<DashboardMetricSnapshot> BuildMetricsAsync(
        ProyectoCafeDbContext db,
        DashboardPeriodWindow window,
        CancellationToken ct)
    {
        var startUtc = window.StartInclusiveUtc;
        var endUtc = window.EndExclusiveUtc;

        var activeLotsQuery = db.Lotes.Where(lote => lote.Estado == "Activo");
        if (startUtc.HasValue)
        {
            activeLotsQuery = activeLotsQuery.Where(lote => lote.FechaCreacion >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            activeLotsQuery = activeLotsQuery.Where(lote => lote.FechaCreacion < endUtc.Value);
        }

        var activeLots = await activeLotsQuery
            .Join(
                db.Productos,
                lote => lote.ProductoId,
                producto => producto.ProductoId,
                (lote, producto) => new ActiveLotRow
                {
                    LoteId = lote.LoteId,
                    CantidadDisponible = lote.CantidadDisponible,
                    UnidadMedida = producto.UnidadMedida,
                })
            .ToListAsync(ct);

        var lotsWithStock = activeLots
            .Where(lote => lote.CantidadDisponible > 0)
            .ToList();

        var availableUnits = lotsWithStock
            .Select(lote => lote.UnidadMedida?.Trim())
            .Where(unidad => !string.IsNullOrWhiteSpace(unidad))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activeLotsCount = activeLots.Count;
        var lotsWithStockCount = lotsWithStock.Count;
        var availableQuantity = lotsWithStock.Sum(lote => lote.CantidadDisponible);
        var availableUnit = availableUnits.Length == 1 ? availableUnits[0] : null;

        var filteredLegacyHarvests = db.CosechasCafe.Where(item => item.Estado != "Anulada");
        if (startUtc.HasValue)
        {
            filteredLegacyHarvests = filteredLegacyHarvests.Where(item => item.FechaCosecha.HasValue && item.FechaCosecha.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredLegacyHarvests = filteredLegacyHarvests.Where(item => item.FechaCosecha.HasValue && item.FechaCosecha.Value < endUtc.Value);
        }

        var filteredOperationalHarvests = db.OperacionesAgricolas
            .Where(item => item.Estado != "Anulada" && item.TipoOperacion == TiposOperacionAgricola.Cosecha);
        if (startUtc.HasValue)
        {
            filteredOperationalHarvests = filteredOperationalHarvests.Where(item => item.FechaOperacion.HasValue && item.FechaOperacion.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredOperationalHarvests = filteredOperationalHarvests.Where(item => item.FechaOperacion.HasValue && item.FechaOperacion.Value < endUtc.Value);
        }

        var filteredRecepciones = db.RecepcionesCafe.Where(item => item.Estado != "Anulada");
        if (startUtc.HasValue)
        {
            filteredRecepciones = filteredRecepciones.Where(item => item.FechaRecepcion.HasValue && item.FechaRecepcion.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredRecepciones = filteredRecepciones.Where(item => item.FechaRecepcion.HasValue && item.FechaRecepcion.Value < endUtc.Value);
        }

        var filteredProcesos = db.ProcesosInventario.Where(item => item.Estado != "Anulado");
        if (startUtc.HasValue)
        {
            filteredProcesos = filteredProcesos.Where(item => item.FechaProceso.HasValue && item.FechaProceso.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredProcesos = filteredProcesos.Where(item => item.FechaProceso.HasValue && item.FechaProceso.Value < endUtc.Value);
        }

        var productionProcessCount = 0;
        productionProcessCount += await filteredLegacyHarvests.CountAsync(ct);
        productionProcessCount += await filteredOperationalHarvests.CountAsync(ct);
        productionProcessCount += await filteredRecepciones.CountAsync(ct);
        productionProcessCount += await filteredProcesos.CountAsync(ct);

        var filteredSales = db.Ventas.Where(venta => venta.Estado == true);
        if (startUtc.HasValue)
        {
            filteredSales = filteredSales.Where(venta => venta.Fecha.HasValue && venta.Fecha.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredSales = filteredSales.Where(venta => venta.Fecha.HasValue && venta.Fecha.Value < endUtc.Value);
        }

        var commercialSummary = await filteredSales
            .GroupBy(_ => 1)
            .Select(group => new CommercialSummaryRow
            {
                ActiveSalesCount = group.Count(),
                ActiveSalesTotal = group.Sum(item => item.Total),
                ActiveCustomersCount = group.Select(item => item.ClienteId).Distinct().Count(),
            })
            .FirstOrDefaultAsync(ct);

        var financeBase = await db.VentaDetalles
            .Join(
                filteredSales,
                detalle => detalle.VentaId,
                venta => venta.VentaId,
                (detalle, _) => detalle)
            .GroupBy(_ => 1)
            .Select(group => new FinanceBaseRow
            {
                Revenue = group.Sum(item => item.Subtotal),
                Cost = group.Sum(item => item.CostoUnitario * item.Cantidad),
            })
            .FirstOrDefaultAsync(ct);

        var filteredCosts = db.Costos.Where(costo => costo.Estado != false);
        if (startUtc.HasValue)
        {
            filteredCosts = filteredCosts.Where(costo => costo.Fecha.HasValue && costo.Fecha.Value >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredCosts = filteredCosts.Where(costo => costo.Fecha.HasValue && costo.Fecha.Value < endUtc.Value);
        }

        var activeCostsTotal = await filteredCosts
            .SumAsync(costo => (decimal?)costo.Monto, ct) ?? 0m;

        var filteredLoteMovimientos = db.LoteMovimientos.AsQueryable();
        if (startUtc.HasValue)
        {
            filteredLoteMovimientos = filteredLoteMovimientos.Where(item => item.Fecha >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            filteredLoteMovimientos = filteredLoteMovimientos.Where(item => item.Fecha < endUtc.Value);
        }

        var traceableLotIds = await filteredLegacyHarvests
            .Select(item => item.LoteId)
            .Concat(filteredOperationalHarvests.Where(item => item.LoteId.HasValue).Select(item => item.LoteId!.Value))
            .Concat(filteredRecepciones.Where(item => item.LoteId.HasValue).Select(item => item.LoteId!.Value))
            .Concat(filteredProcesos.Select(item => item.LoteSalidaId))
            .Concat(filteredLoteMovimientos.Select(item => item.LoteId))
            .Distinct()
            .ToListAsync(ct);

        var activeLotIds = activeLots
            .Select(lote => lote.LoteId)
            .ToHashSet();

        var traceableLotsCount = traceableLotIds.Count(activeLotIds.Contains);
        var traceabilityCoveragePercent = activeLotsCount == 0
            ? 0m
            : Math.Round(traceableLotsCount * 100m / activeLotsCount, 1, MidpointRounding.AwayFromZero);

        var revenue = financeBase?.Revenue ?? 0m;
        var grossMarginAmount = revenue - (financeBase?.Cost ?? 0m);
        var grossMarginPercent = revenue == 0m
            ? 0m
            : Math.Round(grossMarginAmount * 100m / revenue, 1, MidpointRounding.AwayFromZero);

        return new DashboardMetricSnapshot
        {
            Production = new DashboardProductionSummary
            {
                ActiveLotsCount = activeLotsCount,
                LotsWithStockCount = lotsWithStockCount,
                AvailableQuantity = availableQuantity,
                AvailableUnit = availableUnit,
                ProcessRecordsCount = productionProcessCount,
            },
            Commercial = new DashboardCommercialSummary
            {
                ActiveSalesCount = commercialSummary?.ActiveSalesCount ?? 0,
                ActiveSalesTotal = commercialSummary?.ActiveSalesTotal ?? 0m,
                ActiveCustomersCount = commercialSummary?.ActiveCustomersCount ?? 0,
            },
            Finance = new DashboardFinanceSummary
            {
                GrossMarginAmount = grossMarginAmount,
                GrossMarginPercent = grossMarginPercent,
                ActiveCostsTotal = activeCostsTotal,
            },
            Traceability = new DashboardTraceabilitySummary
            {
                ActiveLotsCount = activeLotsCount,
                TraceableLotsCount = traceableLotsCount,
                CoveragePercent = traceabilityCoveragePercent,
            },
        };
    }

    private sealed class ActiveLotRow
    {
        public int LoteId { get; init; }

        public decimal CantidadDisponible { get; init; }

        public string? UnidadMedida { get; init; }
    }

    private sealed class CommercialSummaryRow
    {
        public int ActiveSalesCount { get; init; }

        public decimal ActiveSalesTotal { get; init; }

        public int ActiveCustomersCount { get; init; }
    }

    private sealed class FinanceBaseRow
    {
        public decimal Revenue { get; init; }

        public decimal Cost { get; init; }
    }

    private sealed class DashboardMetricSnapshot
    {
        public required DashboardProductionSummary Production { get; init; }

        public required DashboardCommercialSummary Commercial { get; init; }

        public required DashboardFinanceSummary Finance { get; init; }

        public required DashboardTraceabilitySummary Traceability { get; init; }
    }

    private sealed class DashboardPeriodWindow
    {
        private static readonly string[] AllowedPeriods = ["7d", "30d", "90d", "365d", "all", "custom"];

        public required string Period { get; init; }

        public DateTime? StartInclusiveUtc { get; init; }

        public DateTime? EndExclusiveUtc { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public bool ComparisonAvailable { get; init; }

        public DashboardSummaryWindow ToResponse()
        {
            return new DashboardSummaryWindow
            {
                Period = Period,
                From = From?.ToString("yyyy-MM-dd"),
                To = To?.ToString("yyyy-MM-dd"),
                ComparisonAvailable = ComparisonAvailable,
            };
        }

        public DashboardPeriodWindow? CreatePreviousWindow()
        {
            if (!ComparisonAvailable || !StartInclusiveUtc.HasValue || !EndExclusiveUtc.HasValue)
            {
                return null;
            }

            var duration = EndExclusiveUtc.Value - StartInclusiveUtc.Value;
            var previousEndExclusiveUtc = StartInclusiveUtc.Value;
            var previousStartInclusiveUtc = previousEndExclusiveUtc - duration;

            return new DashboardPeriodWindow
            {
                Period = Period,
                StartInclusiveUtc = previousStartInclusiveUtc,
                EndExclusiveUtc = previousEndExclusiveUtc,
                From = DateOnly.FromDateTime(previousStartInclusiveUtc),
                To = DateOnly.FromDateTime(previousEndExclusiveUtc.AddDays(-1)),
                ComparisonAvailable = true,
            };
        }

        public static DashboardPeriodWindow Resolve(string? period, string? from, string? to, DateTime nowUtc)
        {
            var normalized = string.IsNullOrWhiteSpace(period)
                ? "30d"
                : period.Trim().ToLowerInvariant();

            var hasFrom = !string.IsNullOrWhiteSpace(from);
            var hasTo = !string.IsNullOrWhiteSpace(to);

            if (normalized == "custom" || hasFrom || hasTo)
            {
                if (!hasFrom || !hasTo)
                {
                    throw new ArgumentException("Debes enviar ambos parámetros from y to para un rango personalizado.");
                }

                if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
                {
                    throw new ArgumentException("Formato de fecha inválido. Usa YYYY-MM-DD.");
                }

                if (fromDate > toDate)
                {
                    throw new ArgumentException("El rango personalizado no puede tener from mayor que to.");
                }

                return CreateCustomWindow(fromDate, toDate);
            }

            var endExclusiveUtc = nowUtc.Date.AddDays(1);

            return normalized switch
            {
                "7d" => CreateRollingWindow("7d", 7, endExclusiveUtc),
                "30d" => CreateRollingWindow("30d", 30, endExclusiveUtc),
                "90d" => CreateRollingWindow("90d", 90, endExclusiveUtc),
                "365d" => CreateRollingWindow("365d", 365, endExclusiveUtc),
                "all" => new DashboardPeriodWindow { Period = "all", ComparisonAvailable = false },
                _ => throw new ArgumentException($"Periodo inválido. Usa uno de: {string.Join(", ", AllowedPeriods)}."),
            };
        }

        private static DashboardPeriodWindow CreateRollingWindow(string period, int days, DateTime endExclusiveUtc)
        {
            var startInclusiveUtc = endExclusiveUtc.AddDays(-days);

            return new DashboardPeriodWindow
            {
                Period = period,
                StartInclusiveUtc = startInclusiveUtc,
                EndExclusiveUtc = endExclusiveUtc,
                From = DateOnly.FromDateTime(startInclusiveUtc),
                To = DateOnly.FromDateTime(endExclusiveUtc.AddDays(-1)),
                ComparisonAvailable = true,
            };
        }

        private static DashboardPeriodWindow CreateCustomWindow(DateOnly from, DateOnly to)
        {
            var startInclusiveUtc = from.ToDateTime(TimeOnly.MinValue);
            var endExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return new DashboardPeriodWindow
            {
                Period = "custom",
                StartInclusiveUtc = startInclusiveUtc,
                EndExclusiveUtc = endExclusiveUtc,
                From = from,
                To = to,
                ComparisonAvailable = true,
            };
        }
    }

    public sealed class DashboardSummaryResponse
    {
        public required DashboardSummaryWindow Window { get; init; }

        public DashboardPreviousPeriodSummary? PreviousPeriod { get; init; }

        public required DashboardProductionSummary Production { get; init; }

        public required DashboardCommercialSummary Commercial { get; init; }

        public required DashboardFinanceSummary Finance { get; init; }

        public required DashboardTraceabilitySummary Traceability { get; init; }

        public DateTime GeneratedAtUtc { get; init; }
    }

    public sealed class DashboardSummaryWindow
    {
        public required string Period { get; init; }

        public string? From { get; init; }

        public string? To { get; init; }

        public bool ComparisonAvailable { get; init; }
    }

    public sealed class DashboardPreviousPeriodSummary
    {
        public required DashboardSummaryWindow Window { get; init; }

        public required DashboardProductionSummary Production { get; init; }

        public required DashboardCommercialSummary Commercial { get; init; }

        public required DashboardFinanceSummary Finance { get; init; }

        public required DashboardTraceabilitySummary Traceability { get; init; }
    }

    public sealed class DashboardProductionSummary
    {
        public int ActiveLotsCount { get; init; }

        public int LotsWithStockCount { get; init; }

        public decimal AvailableQuantity { get; init; }

        public string? AvailableUnit { get; init; }

        public int ProcessRecordsCount { get; init; }
    }

    public sealed class DashboardCommercialSummary
    {
        public int ActiveSalesCount { get; init; }

        public decimal ActiveSalesTotal { get; init; }

        public int ActiveCustomersCount { get; init; }
    }

    public sealed class DashboardFinanceSummary
    {
        public decimal GrossMarginAmount { get; init; }

        public decimal GrossMarginPercent { get; init; }

        public decimal ActiveCostsTotal { get; init; }
    }

    public sealed class DashboardTraceabilitySummary
    {
        public int ActiveLotsCount { get; init; }

        public int TraceableLotsCount { get; init; }

        public decimal CoveragePercent { get; init; }
    }
}
