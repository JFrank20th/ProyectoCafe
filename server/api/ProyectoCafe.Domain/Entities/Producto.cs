using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCafe.Domain.Entities;

public sealed class Producto
{
    public int ProductoId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public decimal PrecioInternet { get; set; }

    public decimal PrecioMayorista { get; set; }

    public decimal? Stock { get; set; }

    public bool ManejaLotes { get; set; }

    public required string TipoLote { get; set; }

    public bool? Estado { get; set; }

    public int CategoriaId { get; set; }

    public int? PresentacionId { get; set; }

    public required string UnidadMedida { get; set; }

    public bool PermiteDecimal { get; set; }
}
