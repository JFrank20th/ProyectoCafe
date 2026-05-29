using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.Parcelas;

public sealed class ParcelaRequest
{
    [Required]
    public int FincaId { get; set; }

    [Required]
    public int? ProductoCosechaId { get; set; }

    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [MaxLength(100)]
    public string? Cultivo { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool Estado { get; set; } = true;
}
