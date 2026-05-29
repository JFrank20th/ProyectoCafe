namespace ProyectoCafe.Application.Inventario.Productos;

public sealed class ProductoPrecioTipoVentaResponse
{
    public int TipoVentaId { get; set; }

    public required string TipoVentaCodigo { get; set; }

    public required string TipoVentaNombre { get; set; }

    public decimal Precio { get; set; }

    public bool Estado { get; set; }
}
