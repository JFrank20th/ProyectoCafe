using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

/// <summary>
/// Fila de lectura del listado de lotes, con el total de paquetes y peso neto
/// calculados a partir de sus existencias.
/// </summary>
public sealed class LoteCafeResumenRow
{
    public required int LoteCafeId { get; set; }
    public required string CodigoLote { get; set; }
    public required DateTime FechaProduccion { get; set; }
    public DateTime? FechaEnvasado { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public required EstadoLoteCafe Estado { get; set; }
    public string? Observacion { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? VariedadCafeId { get; set; }
    public int? TipoTuesteCafeId { get; set; }
    public int? TipoProcesoCafeId { get; set; }
    public int TotalPaquetes { get; set; }
    public decimal TotalPesoNetoKg { get; set; }
}

/// <summary>
/// Fila de lectura de existencias. Incluye los datos del catálogo para que la
/// API pueda mostrar el SKU sin depender de consultas adicionales.
/// </summary>
public sealed class ExistenciaCafeRow
{
    public required int LoteCafeExistenciaId { get; set; }
    public required int LoteCafeId { get; set; }
    public required int PresentacionCafeId { get; set; }
    public required string PresentacionCodigo { get; set; }
    public required string PresentacionNombre { get; set; }
    public required EstadoCatalogoCafe PresentacionEstado { get; set; }
    public required int TipoCafeId { get; set; }
    public required string TipoCafeCodigo { get; set; }
    public required string TipoCafeNombre { get; set; }
    public required EstadoCatalogoCafe TipoCafeEstado { get; set; }
    public required int CantidadPaquetes { get; set; }
    public required int PesoUnitarioGramos { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>
/// Fila de lectura del historial de movimientos de un lote.
/// </summary>
public sealed class MovimientoCafeRow
{
    public required int MovimientoLoteCafeId { get; set; }
    public required int LoteCafeExistenciaId { get; set; }
    public required int LoteCafeId { get; set; }
    public required TipoMovimientoLoteCafe TipoMovimiento { get; set; }
    public required int CantidadPaquetes { get; set; }
    public required decimal PesoNetoKg { get; set; }
    public required DateTime FechaMovimiento { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public int? UsuarioId { get; set; }
    public required DateTime CreadoEn { get; set; }
    public required int PresentacionCafeId { get; set; }
    public required string PresentacionCodigo { get; set; }
    public required string PresentacionNombre { get; set; }
    public required int TipoCafeId { get; set; }
    public required string TipoCafeCodigo { get; set; }
    public required string TipoCafeNombre { get; set; }
}

public interface ILoteCafeRepository
{
    Task<bool> CodigoExistsAsync(string codigoLote, CancellationToken ct);

    Task AddLoteAsync(LoteCafeTerminado lote, CancellationToken ct);

    Task<LoteCafeTerminado?> FindLoteAsync(int loteCafeId, CancellationToken ct);

    Task<IReadOnlyList<LoteCafeResumenRow>> ListLotesAsync(CancellationToken ct);

    Task<IReadOnlyList<ExistenciaCafeRow>> ListExistenciasAsync(int loteCafeId, CancellationToken ct);

    Task<LoteCafeExistencia?> FindExistenciaAsync(
        int loteCafeId,
        int presentacionCafeId,
        int tipoCafeId,
        CancellationToken ct);

    Task AddExistenciaAsync(LoteCafeExistencia existencia, CancellationToken ct);

    Task AddMovimientoAsync(MovimientoLoteCafe movimiento, CancellationToken ct);

    Task<IReadOnlyList<MovimientoCafeRow>> ListMovimientosAsync(int loteCafeId, CancellationToken ct);

    Task<IReadOnlyList<MovimientoLoteCafe>> ListMovimientosByReferenciaAsync(
        string referencia,
        TipoMovimientoLoteCafe? tipoMovimiento = null,
        CancellationToken ct = default);

    Task<int> GetTotalPaquetesAsync(int loteCafeId, CancellationToken ct);
}
