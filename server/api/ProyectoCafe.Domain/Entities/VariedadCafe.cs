namespace ProyectoCafe.Domain.Entities;

public sealed class VariedadCafe
{
    public int VariedadCafeId { get; set; }

    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public EstadoCatalogoCafe Estado { get; set; } = EstadoCatalogoCafe.Activo;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}