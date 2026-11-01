using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Ventas;

public sealed class VentaFiltro
{
    public int? NumeroVenta { get; set; }

    public int? ClienteId { get; set; }

    public int? Estado { get; set; }

    public int? EstadoEntrega { get; set; }

    public int? EstadoPago { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }
}

public sealed class CreateVentaDetalleRequest
{
    public int LoteCafeId { get; set; }

    public int PresentacionCafeId { get; set; }

    public int TipoCafeId { get; set; }

    public int PrecioVentaId { get; set; }

    public int CantidadPaquetes { get; set; }
}

public sealed class CreateVentaRequest
{
    public int? ClienteId { get; set; }

    public DateTime? FechaVenta { get; set; }

    public string? Observacion { get; set; }

    public List<CreateVentaDetalleRequest> Detalles { get; set; } = new();
}

public sealed class VentaDetalleDto
{
    public required int VentaDetalleId { get; set; }
    public required int PresentacionCafeId { get; set; }
    public required string PresentacionCodigo { get; set; }
    public required string PresentacionNombre { get; set; }
    public required int TipoCafeId { get; set; }
    public required string TipoCafeCodigo { get; set; }
    public required string TipoCafeNombre { get; set; }
    public required int PrecioVentaId { get; set; }
    public required int CantidadPaquetes { get; set; }
    public required decimal PrecioUnitario { get; set; }
    public required decimal Subtotal { get; set; }
}

public sealed class VentaDto
{
    public required int VentaId { get; set; }
    public required int NumeroVenta { get; set; }
    public required DateTime FechaVenta { get; set; }
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public required EstadoVenta Estado { get; set; }
    public required EstadoEntregaVenta EstadoEntrega { get; set; }
    public DateTime? CanceladoEn { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public string? MotivoAnulacion { get; set; }
    public required EstadoPagoVenta EstadoPago { get; set; }
    public required decimal MontoPagado { get; set; }
    public required decimal SaldoPendiente { get; set; }
    public string? Observacion { get; set; }
    public required decimal Total { get; set; }
    public required DateTime CreadoEn { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
    public List<PagoVentaDto> Pagos { get; set; } = new();
}

public sealed class UpdateEstadoEntregaVentaRequest
{
    public EstadoEntregaVenta EstadoEntrega { get; set; }
}

public sealed class CancelVentaRequest
{
    public string? Motivo { get; set; }
}

public sealed class AnulateVentaRequest
{
    public string? Motivo { get; set; }
}

public sealed class CreatePagoVentaRequest
{
    public decimal Monto { get; set; }

    public DateTime? FechaPago { get; set; }

    public string? MetodoPago { get; set; }

    public string? Referencia { get; set; }
}

public sealed class ResumenFinancieroVentaDto
{
    public required int VentaId { get; set; }
    public required decimal Total { get; set; }
    public required decimal MontoPagado { get; set; }
    public required decimal SaldoPendiente { get; set; }
    public required EstadoPagoVenta EstadoPago { get; set; }
    public List<PagoVentaDto> Pagos { get; set; } = new();
}

public sealed class PagoVentaDto
{
    public required int PagoVentaId { get; set; }
    public required int VentaId { get; set; }
    public required decimal Monto { get; set; }
    public required DateTime FechaPago { get; set; }
    public required string MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public int? UsuarioId { get; set; }
    public required DateTime CreadoEn { get; set; }
}