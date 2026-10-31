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

    public string? Observacion { get; set; }

    public decimal Total { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
}