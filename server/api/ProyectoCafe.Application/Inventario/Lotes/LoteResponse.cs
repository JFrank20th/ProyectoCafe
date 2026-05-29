namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LoteResponse
{
    public int LoteId { get; set; }

    public int ProductoId { get; set; }

    public required string ProductoNombre { get; set; }

    public required string UnidadMedida { get; set; }

    public required string CodigoLote { get; set; }

    public decimal CantidadInicial { get; set; }

    public decimal CantidadDisponible { get; set; }

    public required string Estado { get; set; }

    public DateTime FechaCreacion { get; set; }
}
