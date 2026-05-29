namespace ProyectoCafe.Domain.Entities;

public sealed class LoteMovimiento
{
    public int MovimientoId { get; set; }

    public int LoteId { get; set; }

    public required string Tipo { get; set; }

    public int? VentaId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Cantidad { get; set; }

    public string? Observacion { get; set; }
}
