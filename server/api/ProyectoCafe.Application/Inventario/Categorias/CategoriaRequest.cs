using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Categorias;

public sealed class CategoriaRequest
{
    [Required]
    [MaxLength(100)]
    public string? Nombre { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    public bool Estado { get; set; } = true;
}
