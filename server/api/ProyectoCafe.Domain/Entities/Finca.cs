namespace ProyectoCafe.Domain.Entities;

public sealed class Finca
{
    public int FincaId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string? Ubicacion { get; set; }

    public string? Municipio { get; set; }

    public string? Departamento { get; set; }

    public decimal? AreaHectareas { get; set; }

    public bool? Estado { get; set; }
}
