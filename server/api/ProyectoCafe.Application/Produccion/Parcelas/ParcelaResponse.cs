namespace ProyectoCafe.Application.Produccion.Parcelas;

public sealed class ParcelaResponse
{
    public int ParcelaId { get; set; }

    public int FincaId { get; set; }

    public int? ProductoCosechaId { get; set; }

    public string? ProductoCosechaNombre { get; set; }

    public required string FincaNombre { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string? Cultivo { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool Estado { get; set; }
}
