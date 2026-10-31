namespace ProyectoCafe.Domain.Entities;

public sealed class PrecioVenta
{
    public int PrecioVentaId { get; set; }

    public required string Nombre { get; set; }

    public int PresentacionCafeId { get; set; }

    public PresentacionCafe? PresentacionCafe { get; set; }

    public decimal Precio { get; set; }

    public EstadoCatalogoCafe Estado { get; set; } = EstadoCatalogoCafe.Activo;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}