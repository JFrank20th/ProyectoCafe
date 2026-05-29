namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public sealed class OperacionAgricolaResponse
{
    public int OperacionAgricolaId { get; set; }

    public int ParcelaId { get; set; }

    public required string ParcelaNombre { get; set; }

    public int FincaId { get; set; }

    public required string FincaNombre { get; set; }

    public required string TipoOperacion { get; set; }

    public DateTime? FechaOperacion { get; set; }

    public int? InsumoId { get; set; }

    public string? InsumoNombre { get; set; }

    public int? ProductoId { get; set; }

    public string? ProductoNombre { get; set; }

    public decimal? CantidadInsumo { get; set; }

    public decimal? Cantidad { get; set; }

    public string? Cultivo { get; set; }

    public decimal? Humedad { get; set; }

    public int? LoteId { get; set; }

    public string? CodigoLote { get; set; }

    public decimal CostoInsumo { get; set; }

    public decimal CostoManoObra { get; set; }

    public decimal CostoOtros { get; set; }

    public decimal CostoTotal { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}
