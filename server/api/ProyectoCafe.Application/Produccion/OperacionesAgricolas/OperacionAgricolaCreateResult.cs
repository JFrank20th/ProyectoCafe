namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public sealed class OperacionAgricolaCreateResult
{
    public int OperacionAgricolaId { get; set; }

    public int? LoteId { get; set; }

    public string? CodigoLote { get; set; }

    public decimal CostoTotal { get; set; }
}
