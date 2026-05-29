namespace ProyectoCafe.Domain.Entities;

public sealed class EmpaqueCafeDetalle
{
    public int EmpaqueCafeDetalleId { get; set; }

    public int EmpaqueCafeId { get; set; }

    public int ProductoSalidaId { get; set; }

    public int PresentacionId { get; set; }

    public int LoteSalidaId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PesoGramosUnitario { get; set; }

    public decimal PesoTotalGramos { get; set; }

    public decimal CantidadOrigenAsignada { get; set; }

    public decimal CostoOrigenAsignado { get; set; }

    public decimal CostoEmpaqueAsignado { get; set; }

    public required string Estado { get; set; }
}
