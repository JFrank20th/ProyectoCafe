using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

/// <summary>
/// Persistencia del arranque de seguridad. La capa API decide qué sembrar y en
/// qué entornos; esta clase concentra las consultas y escrituras en la base.
/// </summary>
public sealed class SecurityBootstrapStore(ProyectoCafeDbContext db) : ISecurityBootstrapStore
{
    private readonly ProyectoCafeDbContext _db = db;

    public Task MigrateAsync(CancellationToken ct) => _db.Database.MigrateAsync(ct);

    public async Task<IReadOnlyList<DefaultCredentialCandidate>> GetDefaultCredentialCandidatesAsync(
        CancellationToken ct)
    {
        var candidates = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Estado != UsuarioEstado.Eliminado &&
                        (u.EmailNormalizado == "admin@local" || u.EmailNormalizado == "user@local"))
            .Select(u => new { u.EmailNormalizado, u.PasswordHash })
            .ToListAsync(ct);

        return candidates
            .Select(c => new DefaultCredentialCandidate(c.EmailNormalizado, c.PasswordHash))
            .ToArray();
    }

    public async Task EnsureRolesAsync(
        IReadOnlyCollection<string> roleNames,
        CancellationToken ct)
    {
        var existing = await _db.Roles
            .Select(r => r.Nombre)
            .ToListAsync(ct);

        foreach (var name in roleNames)
        {
            if (existing.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _db.Roles.Add(new Rol { Nombre = name, Estado = RolEstado.Activo });
            existing.Add(name);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task EnsureUsersAsync(
        IReadOnlyCollection<SecurityBootstrapUser> users,
        CancellationToken ct)
    {
        if (users.Count == 0)
        {
            return;
        }

        var rolesByName = await _db.Roles
            .Where(r => r.Estado == RolEstado.Activo)
            .ToDictionaryAsync(r => r.Nombre, r => r, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var seed in users)
        {
            var email = (seed.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) continue;

            // El validador garantiza el formato; este chequeo evita que un
            // valor inválido llegue a la base de datos si se omite la validación.
            if (!PasswordHasher.TryParse(seed.PasswordHash, out _, out _, out _))
            {
                throw new InvalidOperationException(
                    $"Auth:Users[{email}]: PasswordHash no es válido. No se permiten contraseñas en texto plano.");
            }

            var existing = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.EmailNormalizado == email && u.Estado != UsuarioEstado.Eliminado, ct);

            if (existing is null)
            {
                existing = new Usuario
                {
                    Email = email,
                    EmailNormalizado = email,
                    Nombre = (seed.Name ?? email).Trim(),
                    PasswordHash = seed.PasswordHash.Trim(),
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = DateTime.UtcNow,
                };

                _db.Usuarios.Add(existing);
                await _db.SaveChangesAsync(ct);
            }
            else if (!string.Equals(existing.PasswordHash, seed.PasswordHash.Trim(), StringComparison.Ordinal))
            {
                // Mantiene sincronizadas las credenciales de desarrollo cuando
                // se rota el hash en appsettings.Development.json.
                existing.PasswordHash = seed.PasswordHash.Trim();
                existing.ActualizadoEn = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            var roleIds = seed.Roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(r => rolesByName.TryGetValue(r, out var role) ? role.RolId : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToArray();

            if (roleIds.Length == 0) continue;

            var already = await _db.UsuariosRoles
                .Where(ur => ur.UsuarioId == existing.UsuarioId)
                .Select(ur => ur.RolId)
                .ToListAsync(ct);

            foreach (var roleId in roleIds)
            {
                if (already.Contains(roleId)) continue;
                _db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = existing.UsuarioId, RolId = roleId });
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task EnsureRolePermissionsAsync(
        string roleName,
        IReadOnlyCollection<SecurityPermission> permissions,
        CancellationToken ct)
    {
        var roles = await _db.Roles
            .Where(r => r.Estado == RolEstado.Activo)
            .ToListAsync(ct);

        var role = roles.FirstOrDefault(r => string.Equals(r.Nombre, roleName, StringComparison.OrdinalIgnoreCase));
        if (role is null) return;

        var existing = await _db.RolesPermisos
            .Where(rp => rp.RolId == role.RolId)
            .Select(rp => new { rp.Modulo, rp.Accion })
            .ToListAsync(ct);

        foreach (var (modulo, accion) in permissions.Select(p => (p.Modulo, p.Accion)))
        {
            var already = existing.Any(x =>
                string.Equals(x.Modulo, modulo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Accion, accion, StringComparison.OrdinalIgnoreCase));

            if (already) continue;

            _db.RolesPermisos.Add(new RolPermiso
            {
                RolId = role.RolId,
                Modulo = modulo,
                Accion = accion,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
