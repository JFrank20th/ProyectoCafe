using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.Cultivos;

public sealed class CultivoRequest
{
    [Required]
    public int FincaId { get; set; }

    public List<CultivoConfiguracionCosechaRequest> ConfiguracionesCosecha { get; set; } = [];

    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [Required]
    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal? AreaHectareas { get; set; }

    public bool Estado { get; set; } = true;
}
