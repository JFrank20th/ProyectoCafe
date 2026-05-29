namespace ProyectoCafe.Application.Negocio.Ventas;

public sealed class VentaResponse
{
    public int VentaId { get; set; }

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public DateTime? Fecha { get; set; }

    public decimal Total { get; set; }

    public string EstadoPago { get; set; } = "Pagado";

    public int? MetodoPagoId { get; set; }

    public string? MetodoPagoNombre { get; set; }

    public decimal CostoTotal { get; set; }

    public decimal MargenBruto { get; set; }

    public decimal RentabilidadPorcentaje { get; set; }

    public string TipoPrecio { get; set; } = "Normal";

    public bool Estado { get; set; }
}

public sealed class VentaDetalleResponse
{
    public int DetalleId { get; set; }

    public int VentaId { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int? PresentacionId { get; set; }

    public string? PresentacionCodigo { get; set; }

    public string? PresentacionNombre { get; set; }

    public decimal? PresentacionCantidad { get; set; }

    public string? PresentacionUnidadMedida { get; set; }

    public string UnidadMedida { get; set; } = string.Empty;

    public int? LoteId { get; set; }

    public string? CodigoLote { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal CostoTotal { get; set; }

    public decimal MargenUnitario { get; set; }

    public decimal MargenBruto { get; set; }

    public decimal RentabilidadPorcentaje { get; set; }

    public decimal Subtotal { get; set; }
}

public sealed class VentaPagoResponse
{
    public int VentaPagoId { get; set; }

    public int VentaId { get; set; }

    public int MetodoPagoId { get; set; }

    public string MetodoPagoNombre { get; set; } = string.Empty;

    public decimal Monto { get; set; }
}

public sealed class VentaLoteMovimientoResponse
{
    public int MovimientoId { get; set; }

    public int LoteId { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public string UnidadMedida { get; set; } = string.Empty;

    public string CodigoLote { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public decimal Cantidad { get; set; }

    public string? Observacion { get; set; }
}

public sealed class VentaViewResponse
{
    public VentaResponse Venta { get; set; } = new();

    public List<VentaDetalleResponse> Detalles { get; set; } = new();

    public List<VentaPagoResponse> Pagos { get; set; } = new();

    public List<VentaLoteMovimientoResponse> LoteMovimientos { get; set; } = new();
}
