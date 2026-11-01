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

    public async Task<IReadOnlyList<VentaDto>> GetVentasAsync(VentaFiltro? filtro, CancellationToken ct)
    {
        var list = filtro is null
            ? await _ventas.ListAsync(ct)
            : await _ventas.ListFilteredAsync(filtro, ct);
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
        int? usuarioId,
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
        var loteIds = new List<int>();
        decimal total = 0m;

        foreach (var detalleRequest in request.Detalles)
        {
            if (detalleRequest.CantidadPaquetes <= 0)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.CantidadPaquetes),
                    "La cantidad debe ser mayor que cero."));
            }

            if (detalleRequest.LoteCafeId <= 0)
            {
                return OperationResult<int>.Validation(Error(
                    nameof(CreateVentaDetalleRequest.LoteCafeId),
                    "El lote es requerido."));
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

            loteIds.Add(detalleRequest.LoteCafeId);
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var numeroVenta = await _ventas.GetNextNumeroAsync(token);

                var lotesAfectados = new HashSet<int>();

                for (var index = 0; index < detalles.Count; index++)
                {
                    var detalle = detalles[index];
                    var loteCafeId = loteIds[index];

                    var lote = await _lotes.FindLoteAsync(loteCafeId, token);
                    if (lote is null || lote.Estado != EstadoLoteCafe.Activo)
                    {
                        return OperationResult<int>.Validation(Error(
                            nameof(CreateVentaDetalleRequest.LoteCafeId),
                            "El lote no existe o no está activo."));
                    }

                    var existencia = await _lotes.FindExistenciaAsync(
                        loteCafeId,
                        detalle.PresentacionCafeId,
                        detalle.TipoCafeId,
                        token);

                    if (existencia is null || existencia.CantidadPaquetes < detalle.CantidadPaquetes)
                    {
                        return OperationResult<int>.Validation(Error(
                            nameof(CreateVentaDetalleRequest.CantidadPaquetes),
                            "No hay existencias suficientes en el lote seleccionado."));
                    }

                    existencia.CantidadPaquetes -= detalle.CantidadPaquetes;
                    existencia.ActualizadoEn = now;

                    var movimiento = new MovimientoLoteCafe
                    {
                        LoteCafeExistencia = existencia,
                        TipoMovimiento = TipoMovimientoLoteCafe.Salida,
                        CantidadPaquetes = detalle.CantidadPaquetes,
                        PesoNetoKg = Math.Round(
                            (decimal)detalle.CantidadPaquetes * existencia.PesoUnitarioGramos / 1000m,
                            3,
                            MidpointRounding.AwayFromZero),
                        FechaMovimiento = fechaVenta,
                        Referencia = $"VENTA-{numeroVenta}",
                        Observacion = $"Venta {numeroVenta} - {detalle.PresentacionCafe?.Nombre ?? string.Empty} / {detalle.TipoCafe?.Nombre ?? string.Empty}",
                        UsuarioId = usuarioId,
                        CreadoEn = now,
                    };

                    await _lotes.AddMovimientoAsync(movimiento, token);
                    lotesAfectados.Add(loteCafeId);
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
                    EstadoEntrega = EstadoEntregaVenta.PendientePreparacion,
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

    public async Task<OperationResult> UpdateEstadoEntregaAsync(
        int ventaId,
        EstadoEntregaVenta estadoEntrega,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(estadoEntrega))
        {
            return OperationResult.Validation(Error(
                nameof(UpdateEstadoEntregaVentaRequest.EstadoEntrega),
                "El estado de entrega no es válido."));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var venta = await _ventas.FindAsync(ventaId, token);
                if (venta is null)
                {
                    return OperationResult.NotFound();
                }

                if (venta.Estado is EstadoVenta.Cancelada or EstadoVenta.Anulada)
                {
                    return OperationResult.Validation(Error(
                        nameof(UpdateEstadoEntregaVentaRequest.EstadoEntrega),
                        "No se puede actualizar la entrega de una venta cancelada o anulada."));
                }

                venta.EstadoEntrega = estadoEntrega;
                venta.ActualizadoEn = now;

                await _unitOfWork.SaveChangesAsync(token);
                return OperationResult.Success();
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult.Conflict();
        }
    }

    public async Task<OperationResult> CancelVentaAsync(
        int ventaId,
        string motivo,
        int? usuarioId,
        CancellationToken ct)
    {
        return await ChangeSaleStatusAsync(
            ventaId,
            EstadoVenta.Cancelada,
            motivo,
            usuarioId,
            "CANCELACION",
            ct);
    }

    public async Task<OperationResult> AnulateVentaAsync(
        int ventaId,
        string motivo,
        int? usuarioId,
        CancellationToken ct)
    {
        return await ChangeSaleStatusAsync(
            ventaId,
            EstadoVenta.Anulada,
            motivo,
            usuarioId,
            "ANULACION",
            ct);
    }

    private async Task<OperationResult> ChangeSaleStatusAsync(
        int ventaId,
        EstadoVenta nuevoEstado,
        string motivo,
        int? usuarioId,
        string referenciaPrefijo,
        CancellationToken ct)
    {
        var motivoNormalizado = (motivo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(motivoNormalizado))
        {
            return OperationResult.Validation(Error(
                nameof(motivo),
                "El motivo es requerido."));
        }

        if (motivoNormalizado.Length > 250)
        {
            return OperationResult.Validation(Error(
                nameof(motivo),
                "El motivo no puede exceder 250 caracteres."));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var venta = await _ventas.FindAsync(ventaId, token);
                if (venta is null)
                {
                    return OperationResult.NotFound();
                }

                if (venta.Estado != EstadoVenta.Registrada)
                {
                    return OperationResult.Validation(Error(
                        nameof(EstadoVenta),
                        "La venta ya está cancelada o anulada."));
                }

                if (venta.EstadoEntrega == EstadoEntregaVenta.Entregada)
                {
                    return OperationResult.Validation(Error(
                        nameof(EstadoEntregaVenta),
                        "No se puede cancelar o anular una venta entregada."));
                }

                var movimientos = await _lotes.ListMovimientosByReferenciaAsync(
                    $"VENTA-{venta.NumeroVenta}",
                    TipoMovimientoLoteCafe.Salida,
                    token);

                if (movimientos.Count == 0)
                {
                    return OperationResult.Validation(Error(
                        nameof(Venta.NumeroVenta),
                        "No se encontraron movimientos de salida asociados a la venta."));
                }

                var lotesAfectados = new HashSet<int>();

                foreach (var movimiento in movimientos)
                {
                    var existencia = movimiento.LoteCafeExistencia;
                    if (existencia is null)
                    {
                        return OperationResult.Validation(Error(
                            nameof(MovimientoLoteCafe.LoteCafeExistenciaId),
                            "No se pudo identificar la existencia original de la venta."));
                    }

                    existencia.CantidadPaquetes += movimiento.CantidadPaquetes;
                    existencia.ActualizadoEn = now;

                    var movimientoInverso = new MovimientoLoteCafe
                    {
                        LoteCafeExistencia = existencia,
                        TipoMovimiento = TipoMovimientoLoteCafe.AjustePositivo,
                        CantidadPaquetes = movimiento.CantidadPaquetes,
                        PesoNetoKg = movimiento.PesoNetoKg,
                        FechaMovimiento = now,
                        Referencia = $"{referenciaPrefijo}-VENTA-{venta.NumeroVenta}",
                        Observacion = $"{referenciaPrefijo} de venta {venta.NumeroVenta}",
                        UsuarioId = usuarioId,
                        CreadoEn = now,
                    };

                    await _lotes.AddMovimientoAsync(movimientoInverso, token);
                    lotesAfectados.Add(existencia.LoteCafeId);
                }

                foreach (var loteId in lotesAfectados)
                {
                    var lote = await _lotes.FindLoteAsync(loteId, token);
                    if (lote is null)
                    {
                        continue;
                    }

                    if (lote.Estado == EstadoLoteCafe.Agotado)
                    {
                        // La restauracion de esta operacion siempre devuelve paquetes
                        // al lote, por lo que un lote agotado vuelve a estar activo.
                        lote.Estado = EstadoLoteCafe.Activo;
                    }

                    lote.ActualizadoEn = now;
                }

                if (nuevoEstado == EstadoVenta.Cancelada)
                {
                    venta.Estado = EstadoVenta.Cancelada;
                    venta.CanceladoEn = now;
                    venta.CanceladoPorUsuarioId = usuarioId;
                    venta.MotivoCancelacion = motivoNormalizado;
                }
                else
                {
                    venta.Estado = EstadoVenta.Anulada;
                    venta.AnuladoEn = now;
                    venta.AnuladoPorUsuarioId = usuarioId;
                    venta.MotivoAnulacion = motivoNormalizado;
                }

                venta.ActualizadoEn = now;

                await _unitOfWork.SaveChangesAsync(token);
                return OperationResult.Success();
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult.Conflict();
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
            EstadoEntrega = venta.EstadoEntrega,
            CanceladoEn = venta.CanceladoEn,
            MotivoCancelacion = venta.MotivoCancelacion,
            AnuladoEn = venta.AnuladoEn,
            MotivoAnulacion = venta.MotivoAnulacion,
            EstadoPago = venta.EstadoPago,
            MontoPagado = venta.MontoPagado,
            SaldoPendiente = venta.SaldoPendiente,
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
            Pagos = venta.Pagos.Select(pago => new PagoVentaDto
            {
                PagoVentaId = pago.PagoVentaId,
                VentaId = pago.VentaId,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago,
                MetodoPago = pago.MetodoPago,
                Referencia = pago.Referencia,
                UsuarioId = pago.UsuarioId,
                CreadoEn = pago.CreadoEn,
            }).ToList(),
        };

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };
}