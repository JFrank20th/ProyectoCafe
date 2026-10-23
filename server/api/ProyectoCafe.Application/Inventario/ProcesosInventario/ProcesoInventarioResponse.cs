namespace ProyectoCafe.Application.Inventario.ProcesosInventario;

public sealed class ProcesoInventarioResponse
{
    public int ProcesoInventarioId { get; set; }

    public int RecepcionCafeId { get; set; }

    public int? LoteOrigenId { get; set; }

    public required string CodigoLoteOrigen { get; set; }

    public int ProductoOrigenId { get; set; }

    public required string ProductoOrigenNombre { get; set; }

    public required string UnidadMedidaOrigen { get; set; }

    public int ProductoSalidaId { get; set; }

    public required string ProductoSalidaNombre { get; set; }

    public required string UnidadMedidaSalida { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public bool UsaLoteCompleto { get; set; }

    public DateTime? FechaProceso { get; set; }

    public decimal CantidadOrigenConsumida { get; set; }

    public decimal CantidadSalida { get; set; }

    public decimal CostoProceso { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}