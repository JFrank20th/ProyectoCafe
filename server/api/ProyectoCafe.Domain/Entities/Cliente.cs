namespace ProyectoCafe.Domain.Entities;

public sealed class Cliente
{
    public int ClienteId { get; set; }

    public required string Nombre { get; set; }

    public string? Documento { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public bool? Estado { get; set; }

    public DateTime? FechaCreacion { get; set; }
}
