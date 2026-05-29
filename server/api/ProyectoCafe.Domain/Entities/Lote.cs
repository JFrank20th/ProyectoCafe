namespace ProyectoCafe.Domain.Entities;

public sealed class Lote
{
    public int LoteId { get; set; }

    public int ProductoId { get; set; }

    public required string CodigoLote { get; set; }

    public decimal CantidadInicial { get; set; }

    public decimal CantidadDisponible { get; set; }

    public required string Estado { get; set; }

    public DateTime FechaCreacion { get; set; }
}
