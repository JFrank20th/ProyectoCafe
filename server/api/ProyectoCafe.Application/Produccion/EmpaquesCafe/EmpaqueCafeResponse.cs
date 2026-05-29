namespace ProyectoCafe.Application.Produccion.EmpaquesCafe;

public sealed class EmpaqueCafeResponse
{
    public int EmpaqueCafeId { get; set; }

    public int LoteOrigenId { get; set; }

    public required string CodigoLoteOrigen { get; set; }

    public int ProductoOrigenId { get; set; }

    public required string ProductoOrigenNombre { get; set; }

    public required string UnidadMedidaOrigen { get; set; }

    public DateTime? FechaEmpaque { get; set; }

    public decimal CantidadOrigenConsumida { get; set; }

    public decimal CantidadEmpaques { get; set; }

    public decimal PesoTotalGramos { get; set; }

    public decimal CostoEmpaque { get; set; }

    public decimal CostoOrigenTotal { get; set; }

    public decimal CostoTotal { get; set; }

    public string? Observaciones { get; set; }

    public required string Estado { get; set; }

    public IReadOnlyList<EmpaqueCafeDetalleResponse> Detalles { get; set; } = [];
}

public sealed class EmpaqueCafeDetalleResponse
{
    public int EmpaqueCafeDetalleId { get; set; }

    public int ProductoSalidaId { get; set; }

    public required string ProductoSalidaNombre { get; set; }

    public required string UnidadMedidaSalida { get; set; }

    public int PresentacionId { get; set; }

    public required string PresentacionCodigo { get; set; }

    public required string PresentacionNombre { get; set; }

    public decimal PresentacionCantidad { get; set; }

    public required string PresentacionUnidadMedida { get; set; }

    public int LoteSalidaId { get; set; }

    public required string CodigoLoteSalida { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PesoGramosUnitario { get; set; }

    public decimal PesoTotalGramos { get; set; }

    public decimal CantidadOrigenAsignada { get; set; }

    public decimal CostoOrigenAsignado { get; set; }

    public decimal CostoEmpaqueAsignado { get; set; }

    public decimal CostoTotalAsignado { get; set; }

    public decimal CostoUnitarioSalida { get; set; }

    public required string Estado { get; set; }
}
