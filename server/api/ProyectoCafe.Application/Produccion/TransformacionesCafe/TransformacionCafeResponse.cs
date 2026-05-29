namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public sealed class TransformacionCafeResponse
{
    public int TransformacionCafeId { get; set; }

    public int LoteOrigenId { get; set; }

    public required string CodigoLoteOrigen { get; set; }

    public int ProductoOrigenId { get; set; }

    public required string ProductoOrigenNombre { get; set; }

    public required string UnidadMedidaOrigen { get; set; }

    public int ProductoSalidaId { get; set; }

    public required string ProductoSalidaNombre { get; set; }

    public required string UnidadMedidaSalida { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

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
