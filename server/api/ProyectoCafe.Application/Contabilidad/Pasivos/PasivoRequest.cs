using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Contabilidad.Pasivos;

public sealed class PasivoRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; } = true;
}
