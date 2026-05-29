namespace ProyectoCafe.Application.Produccion.EmpaquesCafe;

public sealed class EmpaqueCafeCreateResult
{
    public int EmpaqueCafeId { get; set; }

    public IReadOnlyList<EmpaqueCafeCreateDetailResult> Detalles { get; set; } = [];
}

public sealed class EmpaqueCafeCreateDetailResult
{
    public int ProductoSalidaId { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public decimal Cantidad { get; set; }
}
