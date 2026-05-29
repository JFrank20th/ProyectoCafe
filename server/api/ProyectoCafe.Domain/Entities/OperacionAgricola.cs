namespace ProyectoCafe.Domain.Entities;

public sealed class OperacionAgricola
{
    public int OperacionAgricolaId { get; set; }

    public int ParcelaId { get; set; }

    public required string TipoOperacion { get; set; }

    public DateTime? FechaOperacion { get; set; }

    public int? InsumoId { get; set; }

    public int? ProductoId { get; set; }

    public decimal? CantidadInsumo { get; set; }

    public decimal? Cantidad { get; set; }

    public string? Cultivo { get; set; }

    public decimal? Humedad { get; set; }

    public int? LoteId { get; set; }

    public decimal CostoInsumo { get; set; }

    public decimal CostoManoObra { get; set; }

    public decimal CostoOtros { get; set; }

    public decimal CostoTotal { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}
