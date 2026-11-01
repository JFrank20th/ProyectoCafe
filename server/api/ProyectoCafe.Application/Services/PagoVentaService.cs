using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

public sealed class PagoVentaService(
    IVentaRepository ventas,
    IPagoVentaRepository pagos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly IVentaRepository _ventas = ventas;
    private readonly IPagoVentaRepository _pagos = pagos;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<OperationResult<PagoVentaDto>> RegisterPagoAsync(
        int ventaId,
        CreatePagoVentaRequest request,
        int? usuarioId,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.Monto <= 0m)
        {
            return OperationResult<PagoVentaDto>.Validation(Error(
                nameof(CreatePagoVentaRequest.Monto),
                "El monto del pago debe ser mayor que cero."));
        }

        if (string.IsNullOrWhiteSpace(request.MetodoPago))
        {
            return OperationResult<PagoVentaDto>.Validation(Error(
                nameof(CreatePagoVentaRequest.MetodoPago),
                "El método de pago es requerido."));
        }

        var metodoPago = request.MetodoPago.Trim();
        if (metodoPago.Length > 50)
        {
            return OperationResult<PagoVentaDto>.Validation(Error(
                nameof(CreatePagoVentaRequest.MetodoPago),
                "El método de pago no puede exceder 50 caracteres."));
        }

        if (request.Referencia is { Length: > 100 })
        {
            return OperationResult<PagoVentaDto>.Validation(Error(
                nameof(CreatePagoVentaRequest.Referencia),
                "La referencia no puede exceder 100 caracteres."));
        }

        var fechaPago = request.FechaPago ?? now;
        if (fechaPago > now)
        {
            return OperationResult<PagoVentaDto>.Validation(Error(
                nameof(CreatePagoVentaRequest.FechaPago),
                "La fecha del pago no puede ser futura."));
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var venta = await _ventas.FindWithPagosAsync(ventaId, token);
                if (venta is null)
                {
                    return OperationResult<PagoVentaDto>.NotFound();
                }

                if (venta.Estado is EstadoVenta.Cancelada or EstadoVenta.Anulada)
                {
                    return OperationResult<PagoVentaDto>.Validation(Error(
                        nameof(CreatePagoVentaRequest.Monto),
                        "No se pueden registrar pagos para ventas canceladas o anuladas."));
                }

                var montoPagado = venta.MontoPagado;
                var saldoPendiente = venta.Total - montoPagado;
                if (request.Monto > saldoPendiente)
                {
                    return OperationResult<PagoVentaDto>.Validation(Error(
                        nameof(CreatePagoVentaRequest.Monto),
                        "El pago supera el saldo pendiente de la venta."));
                }

                var pago = new PagoVenta
                {
                    VentaId = venta.VentaId,
                    Monto = request.Monto,
                    FechaPago = fechaPago,
                    MetodoPago = metodoPago,
                    Referencia = NormalizeOptional(request.Referencia),
                    UsuarioId = usuarioId,
                    CreadoEn = now,
                };

                await _pagos.AddAsync(pago, token);

                // Se actualiza la venta para que su RowVersion participe en el
                // control de concurrencia y dos pagos simultáneos no puedan
                // superar el saldo pendiente.
                venta.ActualizadoEn = now;

                await _unitOfWork.SaveChangesAsync(token);

                return OperationResult<PagoVentaDto>.Success(Map(pago));
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<PagoVentaDto>.Conflict();
        }
    }

    public async Task<OperationResult<IReadOnlyList<PagoVentaDto>>> GetPagosAsync(
        int ventaId,
        CancellationToken ct)
    {
        var venta = await _ventas.FindAsync(ventaId, ct);
        if (venta is null)
        {
            return OperationResult<IReadOnlyList<PagoVentaDto>>.NotFound();
        }

        var pagos = await _pagos.ListByVentaAsync(ventaId, ct);
        return OperationResult<IReadOnlyList<PagoVentaDto>>.Success(
            pagos.Select(Map).ToArray());
    }

    public async Task<OperationResult<ResumenFinancieroVentaDto>> GetResumenFinancieroAsync(
        int ventaId,
        CancellationToken ct)
    {
        var venta = await _ventas.FindWithPagosAsync(ventaId, ct);
        if (venta is null)
        {
            return OperationResult<ResumenFinancieroVentaDto>.NotFound();
        }

        return OperationResult<ResumenFinancieroVentaDto>.Success(new ResumenFinancieroVentaDto
        {
            VentaId = venta.VentaId,
            Total = venta.Total,
            MontoPagado = venta.MontoPagado,
            SaldoPendiente = venta.SaldoPendiente,
            EstadoPago = venta.EstadoPago,
            Pagos = venta.Pagos
                .OrderBy(pago => pago.FechaPago)
                .ThenBy(pago => pago.PagoVentaId)
                .Select(Map)
                .ToList(),
        });
    }

    public async Task<OperationResult<decimal>> GetTotalPagadoAsync(
        int ventaId,
        CancellationToken ct)
    {
        var venta = await _ventas.FindWithPagosAsync(ventaId, ct);
        if (venta is null)
        {
            return OperationResult<decimal>.NotFound();
        }

        return OperationResult<decimal>.Success(venta.MontoPagado);
    }

    public async Task<OperationResult<decimal>> GetSaldoPendienteAsync(
        int ventaId,
        CancellationToken ct)
    {
        var venta = await _ventas.FindWithPagosAsync(ventaId, ct);
        if (venta is null)
        {
            return OperationResult<decimal>.NotFound();
        }

        return OperationResult<decimal>.Success(venta.SaldoPendiente);
    }

    public async Task<OperationResult<EstadoPagoVenta>> GetEstadoPagoAsync(
        int ventaId,
        CancellationToken ct)
    {
        var venta = await _ventas.FindWithPagosAsync(ventaId, ct);
        if (venta is null)
        {
            return OperationResult<EstadoPagoVenta>.NotFound();
        }

        return OperationResult<EstadoPagoVenta>.Success(venta.EstadoPago);
    }

    private static PagoVentaDto Map(PagoVenta pago) =>
        new()
        {
            PagoVentaId = pago.PagoVentaId,
            VentaId = pago.VentaId,
            Monto = pago.Monto,
            FechaPago = pago.FechaPago,
            MetodoPago = pago.MetodoPago,
            Referencia = pago.Referencia,
            UsuarioId = pago.UsuarioId,
            CreadoEn = pago.CreadoEn,
        };

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };
}