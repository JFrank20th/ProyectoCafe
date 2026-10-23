namespace ProyectoCafe.Application.Inventario.Productos;

public sealed class ProductoResponse
{
    public int ProductoId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public decimal PrecioInternet { get; set; }

    public decimal PrecioMayorista { get; set; }

    public decimal Stock { get; set; }

    public bool ManejaLotes { get; set; }

    public required string TipoLote { get; set; }

    public bool Estado { get; set; }

    public string? PresentacionCodigo { get; set; }

    public string? PresentacionNombre { get; set; }

    public decimal? PresentacionCantidad { get; set; }

    public string? PresentacionUnidadMedida { get; set; }

    public required string UnidadMedida { get; set; }

    public bool PermiteDecimal { get; set; }

    public IReadOnlyList<ProductoPrecioTipoVentaResponse> PreciosTipoVenta { get; set; } = [];
}
