namespace ProyectoCafe.Domain.Entities;

public sealed class MetodoPago
{
    public int MetodoPagoId { get; set; }

    public required string Nombre { get; set; }

    public bool? Estado { get; set; }
}
