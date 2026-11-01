using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Inventario;

public sealed class CreateLoteCafeRequest
{
    public string? CodigoLote { get; set; }

    public DateTime FechaProduccion { get; set; }

    public DateTime? FechaEnvasado { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public string? Observacion { get; set; }

    public int? VariedadCafeId { get; set; }

    public int? TipoTuesteCafeId { get; set; }

    public int? TipoProcesoCafeId { get; set; }
}

public sealed class RegistrarMovimientoRequest
{
    public TipoMovimientoLoteCafe TipoMovimiento { get; set; }

    public int PresentacionCafeId { get; set; }

    public int TipoCafeId { get; set; }

    public int CantidadPaquetes { get; set; }

    public string? Referencia { get; set; }

    public string? Observacion { get; set; }

    public DateTime? FechaMovimiento { get; set; }
}

public sealed class UpsertPresentacionCafeRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public int? PesoUnitarioGramos { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class UpsertTipoCafeRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class LoteCafeResumenDto
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
    public string? VariedadCafeNombre { get; set; }
    public int? TipoTuesteCafeId { get; set; }
    public string? TipoTuesteCafeNombre { get; set; }
    public int? TipoProcesoCafeId { get; set; }
    public string? TipoProcesoCafeNombre { get; set; }
    public int TotalPaquetes { get; set; }
    public decimal TotalPesoNetoKg { get; set; }
}

public sealed class LoteCafeDetalleDto
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
    public string? VariedadCafeNombre { get; set; }
    public int? TipoTuesteCafeId { get; set; }
    public string? TipoTuesteCafeNombre { get; set; }
    public int? TipoProcesoCafeId { get; set; }
    public string? TipoProcesoCafeNombre { get; set; }
    public int TotalPaquetes { get; set; }
    public decimal TotalPesoNetoKg { get; set; }
}

public sealed class ExistenciaCafeDto
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
    public required decimal PesoNetoKg { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

public sealed class MovimientoCafeDto
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

public sealed class PresentacionCafeDto
{
    public required int PresentacionCafeId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required int PesoUnitarioGramos { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

public sealed class TipoCafeDto
{
    public required int TipoCafeId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

public sealed class UpsertVariedadCafeRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class VariedadCafeDto
{
    public required int VariedadCafeId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

public sealed class UpsertTipoTuesteCafeRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class TipoTuesteCafeDto
{
    public required int TipoTuesteCafeId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

public sealed class UpsertTipoProcesoCafeRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class TipoProcesoCafeDto
{
    public required int TipoProcesoCafeId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}
