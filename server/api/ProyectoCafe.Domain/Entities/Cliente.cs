namespace ProyectoCafe.Domain.Entities;

public sealed class Cliente
{
    public int ClienteId { get; set; }

    public required string TipoIdentificacion { get; set; }

    public required string NumeroIdentificacion { get; set; }

    public required string Nombre { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public string? Observacion { get; set; }

    public EstadoCatalogoCafe Estado { get; set; } = EstadoCatalogoCafe.Activo;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}