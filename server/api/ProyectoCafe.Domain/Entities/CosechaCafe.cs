namespace ProyectoCafe.Domain.Entities;

public sealed class CosechaCafe
{
    public int CosechaCafeId { get; set; }

    public int ParcelaId { get; set; }

    public int ProductoId { get; set; }

    public required string Cultivo { get; set; }

    public DateTime? FechaCosecha { get; set; }

    public decimal Cantidad { get; set; }

    public decimal? Humedad { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }

    public int LoteId { get; set; }
}
