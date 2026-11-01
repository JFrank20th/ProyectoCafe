namespace ProyectoCafe.Domain.Entities;

public enum EstadoLoteCafe
{
    Activo = 1,
    Agotado = 2,
    Bloqueado = 3,
    Eliminado = 4,
}

public sealed class LoteCafeTerminado
{
    public int LoteCafeId { get; set; }

    public required string CodigoLote { get; set; }

    public DateTime FechaProduccion { get; set; }

    public DateTime? FechaEnvasado { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public EstadoLoteCafe Estado { get; set; } = EstadoLoteCafe.Activo;

    public string? Observacion { get; set; }

    public int? VariedadCafeId { get; set; }

    public int? TipoTuesteCafeId { get; set; }

    public int? TipoProcesoCafeId { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<LoteCafeExistencia> Existencias { get; set; } = new List<LoteCafeExistencia>();
}
