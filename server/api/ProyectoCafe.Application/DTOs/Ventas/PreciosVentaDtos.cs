using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Ventas;

public sealed class UpsertPrecioVentaRequest
{
    public string? Nombre { get; set; }

    public int PresentacionCafeId { get; set; }

    public decimal? Precio { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class PrecioVentaDto
{
    public required int PrecioVentaId { get; set; }
    public required string Nombre { get; set; }
    public required int PresentacionCafeId { get; set; }
    public required string PresentacionCodigo { get; set; }
    public required string PresentacionNombre { get; set; }
    public required decimal Precio { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}