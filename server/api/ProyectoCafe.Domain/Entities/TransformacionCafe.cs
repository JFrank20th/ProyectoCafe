namespace ProyectoCafe.Domain.Entities;

public sealed class TransformacionCafe
{
    public int TransformacionCafeId { get; set; }

    public int LoteOrigenId { get; set; }

    public int ProductoSalidaId { get; set; }

    public int LoteSalidaId { get; set; }

    public required string TipoTransformacion { get; set; }

    public DateTime? FechaTransformacion { get; set; }

    public decimal CantidadEntrada { get; set; }

    public decimal CantidadSalida { get; set; }

    public decimal CostoProceso { get; set; }

    public decimal? Merma { get; set; }

    public decimal? Rendimiento { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}
