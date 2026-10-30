namespace ProyectoCafe.Domain.Entities;

public enum EstadoCatalogoCafe
{
    Activo = 1,
    Inactivo = 2,
    Eliminado = 3,
}

public sealed class PresentacionCafe
{
    public int PresentacionCafeId { get; set; }

    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public int PesoUnitarioGramos { get; set; }

    public EstadoCatalogoCafe Estado { get; set; } = EstadoCatalogoCafe.Activo;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
