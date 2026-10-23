using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

/// <summary>
/// Fila de lectura del listado de usuarios. Incluye únicamente los roles activos
/// que la API expone en el módulo de Administración.
/// </summary>
public sealed class UserListRow
{
    public required int UsuarioId { get; set; }
    public required string Email { get; set; }
    public required string Nombre { get; set; }
    public required UsuarioEstado Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? UltimoLoginEn { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public interface IUsuarioRepository
{
    Task<bool> EmailExistsAsync(string emailNormalizado, CancellationToken ct);

    Task<Usuario?> FindByNormalizedEmailAsync(string emailNormalizado, CancellationToken ct);

    Task<Usuario?> FindActiveByIdAsync(int usuarioId, CancellationToken ct);

    Task<Usuario?> FindNotDeletedByIdAsync(int usuarioId, CancellationToken ct);

    Task<bool> IsActiveAsync(int usuarioId, CancellationToken ct);

    Task<bool> HasPermissionAsync(int usuarioId, string modulo, string accion, CancellationToken ct);

    Task AddAsync(Usuario usuario, CancellationToken ct);

    Task<IReadOnlyList<UserListRow>> ListNotDeletedAsync(CancellationToken ct);

    Task<string[]> GetActiveRoleNamesAsync(int usuarioId, CancellationToken ct);

    Task<string[]> GetActivePermissionPoliciesAsync(int usuarioId, string policyPrefix, CancellationToken ct);

    Task ReplaceRolesAsync(int usuarioId, IReadOnlyCollection<int> roleIds, CancellationToken ct);
}
