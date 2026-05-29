using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Compras.Proveedores;

public sealed class ProveedorRequest
{
    [Required]
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(50)]
    public string? Documento { get; set; }

    [MaxLength(50)]
    public string? Telefono { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Direccion { get; set; }

    public bool Estado { get; set; } = true;
}
