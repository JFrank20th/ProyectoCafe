namespace ProyectoCafe.Application.Produccion.Cultivos;

public sealed class CultivoResponse
{
    public int CultivoId { get; set; }

    public int FincaId { get; set; }

    public required string FincaNombre { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool Estado { get; set; }

    public IReadOnlyList<CultivoConfiguracionCosechaResponse> ConfiguracionesCosecha { get; set; } = [];
}
