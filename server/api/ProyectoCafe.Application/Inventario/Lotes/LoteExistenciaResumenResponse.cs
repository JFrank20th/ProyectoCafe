namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LoteExistenciaResumenResponse
{
    public int ProductoId { get; set; }

    public required string ProductoNombre { get; set; }

    public required string UnidadMedida { get; set; }

    public int Lotes { get; set; }

    public int LotesActivos { get; set; }

    public int LotesSinStock { get; set; }

    public decimal CantidadInicial { get; set; }

    public decimal CantidadDisponible { get; set; }

    public DateTime UltimaFechaCreacion { get; set; }
}