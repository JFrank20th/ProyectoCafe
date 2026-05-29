using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.Fincas;

public sealed class FincaRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [MaxLength(200)]
    public string? Ubicacion { get; set; }

    [MaxLength(100)]
    public string? Municipio { get; set; }

    [MaxLength(100)]
    public string? Departamento { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool Estado { get; set; } = true;
}
