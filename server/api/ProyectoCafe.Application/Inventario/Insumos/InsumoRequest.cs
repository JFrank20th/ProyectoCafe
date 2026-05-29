using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Insumos;

public sealed class InsumoRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Cantidad { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CostoUnitario { get; set; }

    [MaxLength(20)]
    public string? UnidadMedida { get; set; }

    public bool Estado { get; set; } = true;
}
