namespace ProyectoCafe.Application.Inventario.Insumos;

public sealed class InsumoResponse
{
    public int InsumoId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public required string UnidadMedida { get; set; }

    public bool Estado { get; set; }
}
