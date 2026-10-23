using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public sealed record RolePermissionRow(int RolId, string Modulo, string Accion);

public interface IRolRepository
{
    Task<IReadOnlyList<Rol>> ListNotDeletedAsync(CancellationToken ct);

    Task<IReadOnlyList<Rol>> ListActiveAsync(CancellationToken ct);

    Task<Rol?> FindNotDeletedAsync(int rolId, CancellationToken ct);

    Task<bool> NameExistsAsync(string nombre, int? excludeRolId, CancellationToken ct);

    Task AddAsync(Rol rol, CancellationToken ct);

    Task<IReadOnlyList<RolePermissionRow>> ListPermissionsByRoleIdsAsync(
        IReadOnlyCollection<int> roleIds,
        CancellationToken ct);

    Task<IReadOnlyList<RolPermiso>> ListPermissionsAsync(int rolId, CancellationToken ct);

    Task ReplacePermissionsAsync(
        int rolId,
        IReadOnlyCollection<(string Modulo, string Accion)> permissions,
        CancellationToken ct);
}
