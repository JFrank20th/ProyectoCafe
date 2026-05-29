namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LoteTraceabilityResponse
{
    public int LoteId { get; set; }

    public int ProductoId { get; set; }

    public required string ProductoNombre { get; set; }

    public required string UnidadMedida { get; set; }

    public required string CodigoLote { get; set; }

    public decimal CantidadInicial { get; set; }

    public decimal CantidadDisponible { get; set; }

    public required string Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public LoteTraceabilityOriginResponse? Origen { get; set; }

    public IReadOnlyList<LoteTraceabilityMovementResponse> Movimientos { get; set; } = [];

    public IReadOnlyList<LoteTraceabilityTransformationResponse> TransformacionesRelacionadas { get; set; } = [];

    public IReadOnlyList<LoteTraceabilityPackagingResponse> EmpaquesRelacionados { get; set; } = [];

    public IReadOnlyList<LoteTraceabilityCostResponse> CostosRelacionados { get; set; } = [];

    public IReadOnlyList<LoteTraceabilityAgriculturalOperationResponse> OperacionesAgricolasRelacionadas { get; set; } = [];
}

public sealed class LoteTraceabilityOriginResponse
{
    public required string Tipo { get; set; }

    public int? ReferenciaId { get; set; }

    public DateTime? Fecha { get; set; }

    public required string Titulo { get; set; }

    public required string Descripcion { get; set; }

    public int? ParcelaId { get; set; }

    public string? ParcelaNombre { get; set; }

    public string? FincaNombre { get; set; }

    public int? LoteOrigenId { get; set; }

    public string? CodigoLoteOrigen { get; set; }
}

public sealed class LoteTraceabilityMovementResponse
{
    public int MovimientoId { get; set; }

    public required string Tipo { get; set; }

    public int? VentaId { get; set; }

    public string? ClienteNombre { get; set; }

    public bool? VentaEstado { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Cantidad { get; set; }

    public string? Observacion { get; set; }
}

public sealed class LoteTraceabilityTransformationResponse
{
    public int TransformacionCafeId { get; set; }

    public required string TipoTransformacion { get; set; }

    public DateTime? FechaTransformacion { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public required string ProductoSalidaNombre { get; set; }

    public decimal CantidadEntrada { get; set; }

    public decimal CantidadSalida { get; set; }

    public decimal CostoProceso { get; set; }

    public required string Estado { get; set; }
}

public sealed class LoteTraceabilityPackagingResponse
{
    public int EmpaqueCafeId { get; set; }

    public required string Rol { get; set; }

    public DateTime? FechaEmpaque { get; set; }

    public int LoteOrigenId { get; set; }

    public required string CodigoLoteOrigen { get; set; }

    public required string ProductoOrigenNombre { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public required string ProductoSalidaNombre { get; set; }

    public required string PresentacionNombre { get; set; }

    public decimal PresentacionCantidad { get; set; }

    public required string PresentacionUnidadMedida { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PesoTotalGramos { get; set; }

    public decimal CantidadOrigenAsignada { get; set; }

    public decimal CostoTotalAsignado { get; set; }

    public required string Estado { get; set; }
}

public sealed class LoteTraceabilityCostResponse
{
    public int CostoId { get; set; }

    public required string Nombre { get; set; }

    public required string TipoCosto { get; set; }

    public string? FuenteModulo { get; set; }

    public int? ReferenciaId { get; set; }

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; }
}

public sealed class LoteTraceabilityAgriculturalOperationResponse
{
    public int OperacionAgricolaId { get; set; }

    public required string TipoOperacion { get; set; }

    public DateTime? FechaOperacion { get; set; }

    public string? InsumoNombre { get; set; }

    public decimal? CantidadInsumo { get; set; }

    public decimal CostoTotal { get; set; }

    public required string Estado { get; set; }
}
