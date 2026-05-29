namespace ProyectoCafe.Domain.Entities;

public sealed class Categoria
{
    public int CategoriaId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public bool? Estado { get; set; }
}
