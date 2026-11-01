namespace ProyectoCafe.Domain.Entities;

public sealed class PagoVenta
{
    public int PagoVentaId { get; set; }

    public int VentaId { get; set; }

    public Venta? Venta { get; set; }

    public decimal Monto { get; set; }

    public DateTime FechaPago { get; set; }

    public required string MetodoPago { get; set; }

    public string? Referencia { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime CreadoEn { get; set; }
}