namespace ProyectoCafe.Application.Negocio.Ventas;

public sealed class VentaCreateRequest
{
    public int ClienteId { get; set; }

    public string EstadoPago { get; set; } = "Pagado";

    public int? MetodoPagoId { get; set; }

    public string TipoPrecio { get; set; } = "Normal";

    public List<VentaCreateDetalleRequest> Detalles { get; set; } = new();
}

public sealed class VentaCreateDetalleRequest
{
    public int ProductoId { get; set; }

    public int? LoteId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal? PrecioUnitario { get; set; }
}
