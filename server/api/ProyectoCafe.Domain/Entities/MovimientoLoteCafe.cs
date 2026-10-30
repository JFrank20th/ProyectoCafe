namespace ProyectoCafe.Domain.Entities;

public enum TipoMovimientoLoteCafe
{
    Ingreso = 1,
    Salida = 2,
    AjustePositivo = 3,
    AjusteNegativo = 4,
}

public sealed class MovimientoLoteCafe
{
    public int MovimientoLoteCafeId { get; set; }

    public int LoteCafeExistenciaId { get; set; }

    public LoteCafeExistencia? LoteCafeExistencia { get; set; }

    public TipoMovimientoLoteCafe TipoMovimiento { get; set; }

    public int CantidadPaquetes { get; set; }

    public decimal PesoNetoKg { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public string? Referencia { get; set; }

    public string? Observacion { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime CreadoEn { get; set; }
}
