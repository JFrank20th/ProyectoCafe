using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Productos;

public sealed class ProductoPrecioTipoVentaRequest
{
    [Required]
    public int TipoVentaId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    public bool Estado { get; set; } = true;
}
