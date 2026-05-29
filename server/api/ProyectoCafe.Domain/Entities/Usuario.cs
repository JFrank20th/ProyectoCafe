namespace ProyectoCafe.Domain.Entities;

public enum UsuarioEstado
{
    PendienteActivacion = 1,
    Activo = 2,
    Inactivo = 3,
    Eliminado = 4,
}

public sealed class Usuario
{
    public int UsuarioId { get; set; }

    public required string Email { get; set; }

    public required string EmailNormalizado { get; set; }

    public required string Nombre { get; set; }

    public required string PasswordHash { get; set; }

    public UsuarioEstado Estado { get; set; } = UsuarioEstado.PendienteActivacion;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public DateTime? UltimoLoginEn { get; set; }

    public int IntentosFallidosLogin { get; set; }

    public DateTime? LockoutFin { get; set; }

    public ICollection<UsuarioRol> UsuariosRoles { get; set; } = new List<UsuarioRol>();
}
