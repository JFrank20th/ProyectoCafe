namespace ProyectoCafe.Application.Inventario.Presentaciones;

public class PresentacionResponse
{
    public int PresentacionId { get; set; }

    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public decimal Cantidad { get; set; }

    public required string UnidadMedida { get; set; }

    public bool Estado { get; set; }
}