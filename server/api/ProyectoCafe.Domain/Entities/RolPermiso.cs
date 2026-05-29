namespace ProyectoCafe.Domain.Entities;

public sealed class RolPermiso
{
    public int RolId { get; set; }

    public Rol? Rol { get; set; }

    public required string Modulo { get; set; }

    public required string Accion { get; set; }
}
