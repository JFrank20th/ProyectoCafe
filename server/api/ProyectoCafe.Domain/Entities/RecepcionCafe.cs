namespace ProyectoCafe.Domain.Entities;

public sealed class RecepcionCafe
{
    public int RecepcionCafeId { get; set; }

    public int ProveedorId { get; set; }

    public int ProductoId { get; set; }

    public required string TipoCafe { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public string? NumeroDocumento { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Total { get; set; }

    public decimal? Humedad { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }

    public int LoteId { get; set; }
}
