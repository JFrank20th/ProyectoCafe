namespace ProyectoCafe.Domain.Entities;

public sealed class TipoVenta
{
    public int TipoVentaId { get; set; }

    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public int Orden { get; set; }

    public bool? Estado { get; set; }
}
