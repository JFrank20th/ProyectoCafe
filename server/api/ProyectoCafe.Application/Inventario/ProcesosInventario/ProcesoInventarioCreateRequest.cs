using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.ProcesosInventario;

public sealed class ProcesoInventarioCreateRequest
{
    [Required]
    public int RecepcionCafeId { get; set; }

    [Required]
    public int ProductoSalidaId { get; set; }

    public bool UsaLoteCompleto { get; set; }

    public DateTime? FechaProceso { get; set; }

    public decimal? CantidadOrigenConsumida { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal CantidadSalida { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostoProceso { get; set; }

    [MaxLength(50)]
    public string? CodigoLoteSalida { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }
}