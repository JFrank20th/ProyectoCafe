using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LoteRequest
{
    [Required]
    public int ProductoId { get; set; }

    [MaxLength(50)]
    public string? CodigoLote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CantidadInicial { get; set; }

    [MaxLength(20)]
    public string? Estado { get; set; }

    // Si es null o vacío, se usa el TipoLote del producto.
    [MaxLength(30)]
    public string? TipoLoteOverride { get; set; }
}
