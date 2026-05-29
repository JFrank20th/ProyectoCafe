namespace ProyectoCafe.Domain.Entities;

public sealed class EmpaqueCafe
{
    public int EmpaqueCafeId { get; set; }

    public int LoteOrigenId { get; set; }

    public DateTime? FechaEmpaque { get; set; }

    public decimal CantidadOrigenConsumida { get; set; }

    public decimal CantidadEmpaques { get; set; }

    public decimal PesoTotalGramos { get; set; }

    public decimal CostoEmpaque { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }
}
