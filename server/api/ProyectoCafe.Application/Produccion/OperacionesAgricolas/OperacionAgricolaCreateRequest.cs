using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public sealed class OperacionAgricolaCreateRequest
{
    [Required]
    public int ParcelaId { get; set; }

    [Required]
    [MaxLength(30)]
    public string? TipoOperacion { get; set; }

    public DateTime? FechaOperacion { get; set; }

    public int? InsumoId { get; set; }

    public int? ProductoId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CantidadInsumo { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal? Cantidad { get; set; }

    [MaxLength(30)]
    public string? Cultivo { get; set; }

    [Range(0, 100)]
    public decimal? Humedad { get; set; }

    [MaxLength(50)]
    public string? CodigoLote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostoManoObra { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostoOtros { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }
}
