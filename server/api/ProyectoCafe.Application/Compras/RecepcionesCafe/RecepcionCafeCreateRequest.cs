using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Compras.RecepcionesCafe;

public sealed class RecepcionCafeCreateRequest
{
    [Required]
    public int ProveedorId { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    [MaxLength(30)]
    public string? TipoCafe { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    [MaxLength(50)]
    public string? NumeroDocumento { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }

    [Range(0, 100)]
    public decimal? Humedad { get; set; }

    [MaxLength(50)]
    public string? CodigoLote { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }
}
