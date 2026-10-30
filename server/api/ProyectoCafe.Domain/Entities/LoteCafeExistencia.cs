namespace ProyectoCafe.Domain.Entities;

public sealed class LoteCafeExistencia
{
    public int LoteCafeExistenciaId { get; set; }

    public int LoteCafeId { get; set; }

    public LoteCafeTerminado? LoteCafe { get; set; }

    public int PresentacionCafeId { get; set; }

    public PresentacionCafe? PresentacionCafe { get; set; }

    public int TipoCafeId { get; set; }

    public TipoCafe? TipoCafe { get; set; }

    public int CantidadPaquetes { get; set; }

    public int PesoUnitarioGramos { get; set; }

    public decimal PesoNetoKg { get; private set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<MovimientoLoteCafe> Movimientos { get; set; } = new List<MovimientoLoteCafe>();
}
