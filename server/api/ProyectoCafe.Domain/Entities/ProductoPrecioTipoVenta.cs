namespace ProyectoCafe.Domain.Entities;

public sealed class ProductoPrecioTipoVenta
{
    public int ProductoPrecioTipoVentaId { get; set; }

    public int ProductoId { get; set; }

    public int TipoVentaId { get; set; }

    public decimal Precio { get; set; }

    public bool? Estado { get; set; }
}
