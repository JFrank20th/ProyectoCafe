using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class RolRepository(ProyectoCafeDbContext db) : IRolRepository
{
    private readonly ProyectoCafeDbContext _db = db;

    public async Task<IReadOnlyList<Rol>> ListNotDeletedAsync(CancellationToken ct) =>
        await _db.Roles
            .Where(r => r.Estado != RolEstado.Eliminado)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rol>> ListActiveAsync(CancellationToken ct) =>
        await _db.Roles
            .Where(r => r.Estado == RolEstado.Activo)
            .ToListAsync(ct);

    public Task<Rol?> FindNotDeletedAsync(int rolId, CancellationToken ct) =>
        _db.Roles.FirstOrDefaultAsync(
            r => r.RolId == rolId && r.Estado != RolEstado.Eliminado,
            ct);

    public Task<bool> NameExistsAsync(string nombre, int? excludeRolId, CancellationToken ct)
    {
        if (excludeRolId is null)
        {
            return _db.Roles.AnyAsync(
                r => r.Estado != RolEstado.Eliminado && r.Nombre == nombre,
                ct);
        }

        var excluded = excludeRolId.Value;

        return _db.Roles.AnyAsync(
            r => r.RolId != excluded && r.Estado != RolEstado.Eliminado && r.Nombre == nombre,
            ct);
    }

    public Task AddAsync(Rol rol, CancellationToken ct)
    {
        _db.Roles.Add(rol);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RolePermissionRow>> ListPermissionsByRoleIdsAsync(
        IReadOnlyCollection<int> roleIds,
        CancellationToken ct)
    {
        if (roleIds.Count == 0)
        {
            return Array.Empty<RolePermissionRow>();
        }

        var ids = roleIds.ToArray();

        return await _db.RolesPermisos
            .Where(rp => ids.Contains(rp.RolId))
            .Select(rp => new RolePermissionRow(rp.RolId, rp.Modulo, rp.Accion))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RolPermiso>> ListPermissionsAsync(int rolId, CancellationToken ct) =>
        await _db.RolesPermisos
            .Where(rp => rp.RolId == rolId)
            .ToListAsync(ct);

    public async Task ReplacePermissionsAsync(
        int rolId,
        IReadOnlyCollection<(string Modulo, string Accion)> permissions,
        CancellationToken ct)
    {
        var existing = await _db.RolesPermisos
            .Where(rp => rp.RolId == rolId)
            .ToListAsync(ct);

        _db.RolesPermisos.RemoveRange(existing);

        foreach (var (modulo, accion) in permissions)
        {
            _db.RolesPermisos.Add(new RolPermiso
            {
                RolId = rolId,
                Modulo = modulo,
                Accion = accion,
            });
        }
    }
}
