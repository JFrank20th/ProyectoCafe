namespace ProyectoCafe.Domain.Entities;

public sealed class VentaDetalle
{
    public int DetalleId { get; set; }

    public int VentaId { get; set; }

    public int ProductoId { get; set; }

    public int? LoteId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal Subtotal { get; set; }
}
