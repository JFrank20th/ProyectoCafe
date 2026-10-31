using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

public sealed class VentaService(
    IVentaRepository ventas,
    IClienteRepository clientes,
    ICatalogoCafeRepository catalogos,
    IPrecioVentaRepository precios,
    ILoteCafeRepository lotes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly IVentaRepository _ventas = ventas;
    private readonly IClienteRepository _clientes = clientes;
    private readonly ICatalogoCafeRepository _catalogos = catalogos;
    private readonly IPrecioVentaRepository _precios = precios;
    private readonly ILoteCafeRepository _lotes = lotes;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<VentaDto>> GetVentasAsync(CancellationToken ct)
    {
        var list = await _ventas.ListAsync(ct);
        return list.Select(Map).ToArray();
    }

    public async Task<OperationResult<VentaDto>> GetVentaAsync(int ventaId, CancellationToken ct)
    {
        var venta = await _ventas.FindAsync(ventaId, ct);
        return venta is null
            ? OperationResult<VentaDto>.NotFound()
            : OperationResult<VentaDto>.Success(Map(venta));
    }

    public async Task<OperationResult<int>> CreateVentaAsync(
        CreateVentaRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            return OperationResult<int>.Validation(Error(
                nameof(CreateVentaRequest.Detalles),
                "La venta debe tener al menos un producto."));
        }

        Cliente? cliente = null;
        if (request.ClienteId.HasValue)
        {
            cliente = await _clientes.FindAsync(request.ClienteId.Value, ct);
            if (cliente is null)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaRequest.ClienteId),
                    "El cliente no existe."));
            }

            if (cliente.Estado != EstadoCatalogoCafe.Activo)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaRequest.ClienteId),
                    "El cliente no está activo."));
            }
        }

        var fechaVenta = request.FechaVenta ?? now;
        if (fechaVenta > now)
        {
            return OperationResult<int>.Validation(Error(
                nameof(CreateVentaRequest.FechaVenta),
                "La fecha de la venta no puede ser futura."));
        }

        var detalles = new List<VentaDetalle>();
        decimal total = 0m;

        foreach (var detalleRequest in request.Detalles)
        {
            if (detalleRequest.CantidadPaquetes <= 0)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.CantidadPaquetes),
                    "La cantidad debe ser mayor que cero."));
            }

            var presentacion = await _catalogos.FindPresentacionAsync(detalleRequest.PresentacionCafeId, ct);
            if (presentacion is null || presentacion.Estado != EstadoCatalogoCafe.Activo)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.PresentacionCafeId),
                    "La presentación no existe o no está activa."));
            }

            var tipoCafe = await _catalogos.FindTipoCafeAsync(detalleRequest.TipoCafeId, ct);
            if (tipoCafe is null || tipoCafe.Estado != EstadoCatalogoCafe.Activo)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.TipoCafeId),
                    "El tipo de café no existe o no está activo."));
            }

            var tarifa = await _precios.FindAsync(detalleRequest.PrecioVentaId, ct);
            if (tarifa is null || tarifa.Estado != EstadoCatalogoCafe.Activo)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.PrecioVentaId),
                    "La tarifa no existe o no está activa."));
            }

            if (tarifa.PresentacionCafeId != detalleRequest.PresentacionCafeId)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.PrecioVentaId),
                    "La tarifa no corresponde a la presentación."));
            }

            var subtotal = tarifa.Precio * detalleRequest.CantidadPaquetes;
            total += subtotal;

            detalles.Add(new VentaDetalle
            {
                PresentacionCafeId = detalleRequest.PresentacionCafeId,
                PresentacionCafe = presentacion,
                TipoCafeId = detalleRequest.TipoCafeId,
                TipoCafe = tipoCafe,
                PrecioVentaId = detalleRequest.PrecioVentaId,
                PrecioVenta = tarifa,
                CantidadPaquetes = detalleRequest.CantidadPaquetes,
                PrecioUnitario = tarifa.Precio,
                Subtotal = subtotal,
                CreadoEn = now,
            });
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var numeroVenta = await _ventas.GetNextNumeroAsync(token);

                var lotes = await _lotes.ListLotesAsync(token);
                var lotesOrdenados = lotes
                    .Where(lote => lote.Estado == EstadoLoteCafe.Activo)
                    .OrderBy(lote => lote.FechaProduccion)
                    .ThenBy(lote => lote.LoteCafeId)
                    .ToArray();

                var lotesAfectados = new HashSet<int>();

                foreach (var detalle in detalles)
                {
                    var remaining = detalle.CantidadPaquetes;

                    foreach (var loteResumen in lotesOrdenados)
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }

                        var existencia = await _lotes.FindExistenciaAsync(
                            loteResumen.LoteCafeId,
                            detalle.PresentacionCafeId,
                            detalle.TipoCafeId,
                            token);

                        if (existencia is null || existencia.CantidadPaquetes <= 0)
                        {
                            continue;
                        }

                        var take = Math.Min(remaining, existencia.CantidadPaquetes);
                        if (take <= 0)
                        {
                            continue;
                        }

                        existencia.CantidadPaquetes -= take;
                        existencia.ActualizadoEn = now;

                        var movimiento = new MovimientoLoteCafe
                        {
                            LoteCafeExistencia = existencia,
                            TipoMovimiento = TipoMovimientoLoteCafe.Salida,
                            CantidadPaquetes = take,
                            PesoNetoKg = Math.Round(
                                (decimal)take * existencia.PesoUnitarioGramos / 1000m,
                                3,
                                MidpointRounding.AwayFromZero),
                            FechaMovimiento = fechaVenta,
                            Referencia = $"VENTA-{numeroVenta}",
                            Observacion = $"Venta {numeroVenta} - {detalle.PresentacionCafe?.Nombre ?? string.Empty} / {detalle.TipoCafe?.Nombre ?? string.Empty}",
                            CreadoEn = now,
                        };

                        await _lotes.AddMovimientoAsync(movimiento, token);
                        lotesAfectados.Add(loteResumen.LoteCafeId);
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        return OperationResult<int>.Validation(Error(
                            nameof(CreateVentaDetalleRequest.CantidadPaquetes),
                            "No hay existencias suficientes para completar la venta."));
                    }
                }

                foreach (var loteId in lotesAfectados)
                {
                    var lote = await _lotes.FindLoteAsync(loteId, token);
                    if (lote is null)
                    {
                        continue;
                    }

                    var totalPaquetes = await _lotes.GetTotalPaquetesAsync(loteId, token);
                    var nuevoEstado = lote.Estado;

                    if (totalPaquetes == 0 && lote.Estado == EstadoLoteCafe.Activo)
                    {
                        nuevoEstado = EstadoLoteCafe.Agotado;
                    }
                    else if (totalPaquetes > 0 && lote.Estado == EstadoLoteCafe.Agotado)
                    {
                        nuevoEstado = EstadoLoteCafe.Activo;
                    }

                    if (nuevoEstado != lote.Estado)
                    {
                        lote.Estado = nuevoEstado;
                    }

                    lote.ActualizadoEn = now;
                }

                var venta = new Venta
                {
                    NumeroVenta = numeroVenta,
                    FechaVenta = fechaVenta,
                    ClienteId = request.ClienteId,
                    Cliente = cliente,
                    Estado = EstadoVenta.Registrada,
                    Observacion = NormalizeOptional(request.Observacion),
                    Total = total,
                    CreadoEn = now,
                    Detalles = detalles,
                };

                await _ventas.AddAsync(venta, token);
                await _unitOfWork.SaveChangesAsync(token);

                return OperationResult<int>.Success(venta.VentaId);
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static VentaDto Map(Venta venta) =>
        new()
        {
            VentaId = venta.VentaId,
            NumeroVenta = venta.NumeroVenta,
            FechaVenta = venta.FechaVenta,
            ClienteId = venta.ClienteId,
            ClienteNombre = venta.Cliente?.Nombre,
            Estado = venta.Estado,
            Observacion = venta.Observacion,
            Total = venta.Total,
            CreadoEn = venta.CreadoEn,
            Detalles = venta.Detalles.Select(detail => new VentaDetalleDto
            {
                VentaDetalleId = detail.VentaDetalleId,
                PresentacionCafeId = detail.PresentacionCafeId,
                PresentacionCodigo = detail.PresentacionCafe?.Codigo ?? string.Empty,
                PresentacionNombre = detail.PresentacionCafe?.Nombre ?? string.Empty,
                TipoCafeId = detail.TipoCafeId,
                TipoCafeCodigo = detail.TipoCafe?.Codigo ?? string.Empty,
                TipoCafeNombre = detail.TipoCafe?.Nombre ?? string.Empty,
                PrecioVentaId = detail.PrecioVentaId,
                CantidadPaquetes = detail.CantidadPaquetes,
                PrecioUnitario = detail.PrecioUnitario,
                Subtotal = detail.Subtotal,
            }).ToList(),
        };

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };
}