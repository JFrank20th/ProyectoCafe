namespace ProyectoCafe.Domain.Entities;

public sealed class Costo
{
    public int CostoId { get; set; }

    public required string Nombre { get; set; }

    public required string TipoCosto { get; set; }

    public string? FuenteModulo { get; set; }

    public int? ReferenciaId { get; set; }

    public int? CultivoId { get; set; }

    public int? LoteId { get; set; }

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool? Estado { get; set; }
}
