using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class UsuarioRepository(ProyectoCafeDbContext db) : IUsuarioRepository
{
    private readonly ProyectoCafeDbContext _db = db;

    public Task<bool> EmailExistsAsync(string emailNormalizado, CancellationToken ct) =>
        _db.Usuarios.AnyAsync(
            u => u.EmailNormalizado == emailNormalizado && u.Estado != UsuarioEstado.Eliminado,
            ct);

    public Task<Usuario?> FindByNormalizedEmailAsync(string emailNormalizado, CancellationToken ct) =>
        _db.Usuarios.FirstOrDefaultAsync(
            u => u.EmailNormalizado == emailNormalizado && u.Estado != UsuarioEstado.Eliminado,
            ct);

    public Task<Usuario?> FindActiveByIdAsync(int usuarioId, CancellationToken ct) =>
        _db.Usuarios.FirstOrDefaultAsync(
            u => u.UsuarioId == usuarioId && u.Estado == UsuarioEstado.Activo,
            ct);

    public Task<Usuario?> FindNotDeletedByIdAsync(int usuarioId, CancellationToken ct) =>
        _db.Usuarios.FirstOrDefaultAsync(
            u => u.UsuarioId == usuarioId && u.Estado != UsuarioEstado.Eliminado,
            ct);

    public Task<bool> IsActiveAsync(int usuarioId, CancellationToken ct) =>
        _db.Usuarios.AnyAsync(
            u => u.UsuarioId == usuarioId && u.Estado == UsuarioEstado.Activo,
            ct);

    public Task<bool> HasPermissionAsync(
        int usuarioId,
        string modulo,
        string accion,
        CancellationToken ct) =>
        _db.UsuariosRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Join(_db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
            .Where(r => r.Estado == RolEstado.Activo)
            .Join(_db.RolesPermisos, r => r.RolId, rp => rp.RolId, (_, rp) => rp)
            .AnyAsync(rp => rp.Modulo == modulo && rp.Accion == accion, ct);

    public Task AddAsync(Usuario usuario, CancellationToken ct)
    {
        _db.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserListRow>> ListNotDeletedAsync(CancellationToken ct)
    {
        var users = await _db.Usuarios
            .Where(u => u.Estado != UsuarioEstado.Eliminado)
            .OrderByDescending(u => u.CreadoEn)
            .Select(u => new UserListRow
            {
                UsuarioId = u.UsuarioId,
                Email = u.Email,
                Nombre = u.Nombre,
                Estado = u.Estado,
                CreadoEn = u.CreadoEn,
                UltimoLoginEn = u.UltimoLoginEn,
            })
            .ToListAsync(ct);

        if (users.Count == 0)
        {
            return users;
        }

        var userIds = users.Select(u => u.UsuarioId).ToArray();
        var roles = await _db.UsuariosRoles
            .Where(ur => userIds.Contains(ur.UsuarioId))
            .Join(_db.Roles, ur => ur.RolId, r => r.RolId, (ur, r) => new { ur.UsuarioId, r.Nombre, r.Estado })
            .Where(x => x.Estado == RolEstado.Activo)
            .ToListAsync(ct);

        var rolesByUser = roles
            .GroupBy(x => x.UsuarioId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray());

        foreach (var user in users)
        {
            user.Roles = rolesByUser.TryGetValue(user.UsuarioId, out var userRoles)
                ? userRoles
                : Array.Empty<string>();
        }

        return users;
    }

    public Task<string[]> GetActiveRoleNamesAsync(int usuarioId, CancellationToken ct) =>
        _db.UsuariosRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Join(_db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
            .Where(r => r.Estado == RolEstado.Activo)
            .Select(r => r.Nombre)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(ct);

    public Task<string[]> GetActivePermissionPoliciesAsync(
        int usuarioId,
        string policyPrefix,
        CancellationToken ct) =>
        _db.UsuariosRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Join(_db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
            .Where(r => r.Estado == RolEstado.Activo)
            .Join(_db.RolesPermisos, r => r.RolId, rp => rp.RolId, (_, rp) => rp)
            .Select(rp => policyPrefix + rp.Modulo + ":" + rp.Accion)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(ct);

    public async Task ReplaceRolesAsync(
        int usuarioId,
        IReadOnlyCollection<int> roleIds,
        CancellationToken ct)
    {
        var existing = await _db.UsuariosRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .ToListAsync(ct);

        _db.UsuariosRoles.RemoveRange(existing);

        foreach (var roleId in roleIds)
        {
            _db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuarioId, RolId = roleId });
        }
    }
}
