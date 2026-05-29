using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.UnidadesMedida;

public sealed class UnidadMedidaRequest
{
    [Required]
    [MaxLength(20)]
    public string? Codigo { get; set; }

    [Required]
    [MaxLength(80)]
    public string? Nombre { get; set; }

    public bool Estado { get; set; } = true;
}
