namespace ProyectoCafe.Domain.Entities;

public sealed class VentaDetalle
{
    public int VentaDetalleId { get; set; }

    public int VentaId { get; set; }

    public Venta? Venta { get; set; }

    public int PresentacionCafeId { get; set; }

    public PresentacionCafe? PresentacionCafe { get; set; }

    public int TipoCafeId { get; set; }

    public TipoCafe? TipoCafe { get; set; }

    public int PrecioVentaId { get; set; }

    public PrecioVenta? PrecioVenta { get; set; }

    public int CantidadPaquetes { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public DateTime CreadoEn { get; set; }
}