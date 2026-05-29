namespace ProyectoCafe.Application.Inventario.UnidadesMedida;

public sealed class UnidadMedidaResponse
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public bool Estado { get; set; }
}
