namespace ProyectoCafe.Domain.Entities;

public sealed class Venta
{
    public int VentaId { get; set; }

    public int ClienteId { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal Total { get; set; }

    public required string EstadoPago { get; set; }

    public required string TipoPrecio { get; set; }

    public bool? Estado { get; set; }
}
