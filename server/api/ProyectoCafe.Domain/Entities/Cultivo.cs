namespace ProyectoCafe.Domain.Entities;

public sealed class Cultivo
{
    public int CultivoId { get; set; }

    public int FincaId { get; set; }

    public int? ProductoCosechaId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool? Estado { get; set; }

    public List<CultivoConfiguracionCosecha> ConfiguracionesCosecha { get; set; } = [];
}
