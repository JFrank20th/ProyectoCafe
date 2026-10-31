using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Ventas;

public sealed class CreateVentaDetalleRequest
{
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
    public string? Observacion { get; set; }
    public required decimal Total { get; set; }
    public required DateTime CreadoEn { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
}