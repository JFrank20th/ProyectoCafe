using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Presentaciones;

public class PresentacionRequest
{
    [Required]
    [MaxLength(30)]
    public string? Codigo { get; set; }

    [Required]
    [MaxLength(120)]
    public string? Nombre { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required]
    [MaxLength(20)]
    public string? UnidadMedida { get; set; }

    public bool Estado { get; set; } = true;
}