namespace ProyectoCafe.Application.Inventario.ProcesosInventario;

public sealed class ProcesoInventarioCreateResult
{
    public int ProcesoInventarioId { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public decimal CantidadOrigenConsumida { get; set; }
}