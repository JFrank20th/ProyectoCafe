using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Contabilidad.Costos;

public sealed class CostoRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(30)]
    public string? TipoCosto { get; set; }

    [MaxLength(120)]
    public string? FuenteModulo { get; set; }

    public int? ReferenciaId { get; set; }

    public int? ParcelaId { get; set; }

    public int? LoteId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; } = true;
}
