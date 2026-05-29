namespace ProyectoCafe.Application.Negocio.MetodosPago;

public sealed class MetodoPagoResponse
{
    public int MetodoPagoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Estado { get; set; }
}
