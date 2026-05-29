namespace ProyectoCafe.Application.Inventario.Categorias;

public sealed class CategoriaResponse
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }
}
