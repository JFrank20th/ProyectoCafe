using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Negocio.TiposVenta;

public sealed class TipoVentaRequest
{
    [Required]
    [MaxLength(30)]
    public string? Codigo { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Nombre { get; set; }

    public int? Orden { get; set; }

    public bool Estado { get; set; } = true;
}
