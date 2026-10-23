using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Administration;

public sealed class UserDto
{
    public required int UsuarioId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required UsuarioEstado Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? UltimoLoginEn { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public sealed class UpdateUserRolesRequest
{
    public string[]? Roles { get; set; }
}

public sealed class RoleDto
{
    public required int RolId { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public required RolEstado Estado { get; set; }
    public int PermisosCount { get; set; }
}

public sealed class UpsertRoleRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public RolEstado? Estado { get; set; }
}

public sealed class PermissionDto
{
    public string? Modulo { get; set; }
    public string? Accion { get; set; }
}

public sealed class UpdateRolePermissionsRequest
{
    public PermissionDto[]? Permissions { get; set; }
}
