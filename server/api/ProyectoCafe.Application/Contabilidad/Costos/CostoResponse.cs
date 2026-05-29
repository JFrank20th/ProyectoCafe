namespace ProyectoCafe.Application.Contabilidad.Costos;

public sealed class CostoResponse
{
    public int CostoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string TipoCosto { get; set; } = string.Empty;

    public string? FuenteModulo { get; set; }

    public int? ReferenciaId { get; set; }

    public int? ParcelaId { get; set; }

    public string? ParcelaNombre { get; set; }

    public int? FincaId { get; set; }

    public string? FincaNombre { get; set; }

    public int? LoteId { get; set; }

    public string? CodigoLote { get; set; }

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; }
}
