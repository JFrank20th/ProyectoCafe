namespace ProyectoCafe.Domain.Entities;

public enum RolEstado
{
    Activo = 0,
    Inactivo = 1,
    Eliminado = 2,
}

public sealed class Rol
{
    public int RolId { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public RolEstado Estado { get; set; } = RolEstado.Activo;

    public ICollection<UsuarioRol> UsuariosRoles { get; set; } = new List<UsuarioRol>();

    public ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}
