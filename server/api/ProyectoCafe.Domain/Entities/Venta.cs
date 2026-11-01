namespace ProyectoCafe.Domain.Entities;

public enum EstadoVenta
{
    Registrada = 1,
    Cancelada = 2,
    Anulada = 3,
}

public sealed class Venta
{
    public int VentaId { get; set; }

    public int NumeroVenta { get; set; }

    public DateTime FechaVenta { get; set; }

    public int? ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.Registrada;

    public EstadoEntregaVenta EstadoEntrega { get; set; } = EstadoEntregaVenta.PendientePreparacion;

    public DateTime? CanceladoEn { get; set; }

    public int? CanceladoPorUsuarioId { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? AnuladoEn { get; set; }

    public int? AnuladoPorUsuarioId { get; set; }

    public string? MotivoAnulacion { get; set; }

    public decimal MontoPagado => Pagos.Sum(pago => pago.Monto);

    public decimal SaldoPendiente => Total - MontoPagado;

    public EstadoPagoVenta EstadoPago =>
        MontoPagado <= 0m
            ? EstadoPagoVenta.PendientePago
            : MontoPagado >= Total
                ? EstadoPagoVenta.Pagada
                : EstadoPagoVenta.AbonoParcial;

    public string? Observacion { get; set; }

    public decimal Total { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();

    public ICollection<PagoVenta> Pagos { get; set; } = new List<PagoVenta>();
}