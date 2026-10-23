namespace ProyectoCafe.Domain.Entities;

public sealed class ProcesoInventario
{
    public int ProcesoInventarioId { get; set; }

    public int RecepcionCafeId { get; set; }

    public int? LoteOrigenId { get; set; }

    public int ProductoSalidaId { get; set; }

    public int LoteSalidaId { get; set; }

    public bool UsaLoteCompleto { get; set; }

    public DateTime? FechaProceso { get; set; }

    public decimal CantidadOrigenConsumida { get; set; }

    public decimal CantidadSalida { get; set; }

    public decimal CostoProceso { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}