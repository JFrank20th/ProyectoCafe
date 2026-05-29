namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public sealed class TransformacionCafeCreateResult
{
    public int TransformacionCafeId { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }
}
