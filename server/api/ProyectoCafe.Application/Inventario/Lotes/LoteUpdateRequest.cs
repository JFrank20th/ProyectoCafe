using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LoteUpdateRequest
{
    [MaxLength(50)]
    public string? CodigoLote { get; set; }

    [MaxLength(20)]
    public string? Estado { get; set; }
}
