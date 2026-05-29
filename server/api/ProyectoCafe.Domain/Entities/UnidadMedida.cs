namespace ProyectoCafe.Domain.Entities;

public sealed class UnidadMedida
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public bool? Estado { get; set; }
}
