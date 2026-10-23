namespace ProyectoCafe.Application.Abstractions.Persistence;

public sealed record SecurityBootstrapUser(
    string Email,
    string Name,
    string PasswordHash,
    IReadOnlyCollection<string> Roles);

public sealed record DefaultCredentialCandidate(string EmailNormalizado, string PasswordHash);

public sealed record SecurityPermission(string Modulo, string Accion);

/// <summary>
/// Operaciones de persistencia necesarias para preparar la seguridad inicial.
/// La orquestación y las reglas de entorno permanecen en la API.
/// </summary>
public interface ISecurityBootstrapStore
{
    Task MigrateAsync(CancellationToken ct);

    Task<IReadOnlyList<DefaultCredentialCandidate>> GetDefaultCredentialCandidatesAsync(CancellationToken ct);

    Task EnsureRolesAsync(IReadOnlyCollection<string> roleNames, CancellationToken ct);

    Task EnsureUsersAsync(IReadOnlyCollection<SecurityBootstrapUser> users, CancellationToken ct);

    Task EnsureRolePermissionsAsync(
        string roleName,
        IReadOnlyCollection<SecurityPermission> permissions,
        CancellationToken ct);
}
