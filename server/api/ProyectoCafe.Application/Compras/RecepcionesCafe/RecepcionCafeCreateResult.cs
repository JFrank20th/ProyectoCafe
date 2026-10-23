namespace ProyectoCafe.Application.Compras.RecepcionesCafe;

public sealed class RecepcionCafeCreateResult
{
    public int RecepcionCafeId { get; set; }

    public required string CodigoLote { get; set; }
}
