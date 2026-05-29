using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Productos;

public sealed class ProductoRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecioInternet { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecioMayorista { get; set; }

    public decimal? Stock { get; set; }

    public bool ManejaLotes { get; set; }

    [MaxLength(30)]
    public string? TipoLote { get; set; }

    public bool Estado { get; set; } = true;

    [Required]
    public int CategoriaId { get; set; }

    [Required]
    public int? PresentacionId { get; set; }

    [MaxLength(20)]
    public string? UnidadMedida { get; set; }

    public bool PermiteDecimal { get; set; }

    public List<ProductoPrecioTipoVentaRequest>? PreciosTipoVenta { get; set; }
}
