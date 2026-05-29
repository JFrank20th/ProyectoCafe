namespace ProyectoCafe.Domain.Entities;

public sealed class VentaPago
{
    public int VentaPagoId { get; set; }

    public int VentaId { get; set; }

    public int MetodoPagoId { get; set; }

    public decimal Monto { get; set; }
}
