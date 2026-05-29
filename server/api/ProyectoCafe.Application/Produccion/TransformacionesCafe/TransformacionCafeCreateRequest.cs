using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public sealed class TransformacionCafeCreateRequest
{
    [Required]
    public int LoteOrigenId { get; set; }

    [Required]
    public int ProductoSalidaId { get; set; }

    [Required]
    [MaxLength(30)]
    public string? TipoTransformacion { get; set; }

    public DateTime? FechaTransformacion { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal CantidadEntrada { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal CantidadSalida { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostoProceso { get; set; }

    [MaxLength(50)]
    public string? CodigoLoteSalida { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }
}
