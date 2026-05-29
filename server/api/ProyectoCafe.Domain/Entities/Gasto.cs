namespace ProyectoCafe.Domain.Entities;

public sealed class Gasto
{
    public int GastoId { get; set; }

    public required string Nombre { get; set; }

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool? Estado { get; set; }
}
