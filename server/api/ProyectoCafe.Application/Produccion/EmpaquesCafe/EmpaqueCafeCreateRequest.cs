using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.EmpaquesCafe;

public sealed class EmpaqueCafeCreateRequest
{
    [Required]
    public int LoteOrigenId { get; set; }

    public DateTime? FechaEmpaque { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostoEmpaque { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }

    [MinLength(1)]
    public List<EmpaqueCafeDetalleCreateRequest> Detalles { get; set; } = [];
}

public sealed class EmpaqueCafeDetalleCreateRequest
{
    [Required]
    public int ProductoSalidaId { get; set; }

    [Required]
    public int PresentacionId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [MaxLength(50)]
    public string? CodigoLoteSalida { get; set; }
}
