using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Auth;

public sealed class SecurityBootstrapHostedService(
    IServiceProvider serviceProvider,
    IOptions<AuthOptions> authOptions,
    IHostEnvironment environment,
    ILogger<SecurityBootstrapHostedService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger<SecurityBootstrapHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

            if (_environment.IsDevelopment())
            {
                await db.Database.MigrateAsync(cancellationToken);
            }

            await EnsureCultivosPermissionsMigratedAsync(db, cancellationToken);
            await EnsureBootstrapRolesAsync(db, cancellationToken);
            await EnsureBootstrapUsersAsync(db, cancellationToken);
            await EnsureAdminPermissionsAsync(db, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security bootstrap no pudo ejecutarse. La API seguirá iniciando, pero auth/admin puede fallar hasta que la BD esté lista.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureCultivosPermissionsMigratedAsync(ProyectoCafeDbContext db, CancellationToken ct)
    {
        var legacyModule = Permissions.Modulos.ProduccionParcelasLegacy;
        var newModule = Permissions.Modulos.ProduccionCultivos;

        var legacyPermissions = await db.RolesPermisos
            .Where(rp => rp.Modulo == legacyModule)
            .Select(rp => new { rp.RolId, rp.Accion })
            .ToListAsync(ct);

        if (legacyPermissions.Count == 0)
        {
            return;
        }

        var existingNewPermissions = await db.RolesPermisos
            .Where(rp => rp.Modulo == newModule)
            .Select(rp => new { rp.RolId, rp.Accion })
            .ToListAsync(ct);

        var existingKeys = existingNewPermissions
            .Select(x => $"{x.RolId}:{x.Accion}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var permission in legacyPermissions)
        {
            var key = $"{permission.RolId}:{permission.Accion}";
            if (existingKeys.Contains(key))
            {
                continue;
            }

            db.RolesPermisos.Add(new RolPermiso
            {
                RolId = permission.RolId,
                Modulo = newModule,
                Accion = permission.Accion,
            });

            existingKeys.Add(key);
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureBootstrapRolesAsync(ProyectoCafeDbContext db, CancellationToken ct)
    {
        var roleNames = _authOptions.Users
            .SelectMany(u => u.Roles)
            .Concat(["Admin", "User"])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roleNames.Length == 0)
        {
            roleNames = ["Admin", "User"];
        }

        var existing = await db.Roles
            .Select(r => r.Nombre)
            .ToListAsync(ct);

        foreach (var name in roleNames)
        {
            if (existing.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            db.Roles.Add(new Rol { Nombre = name, Estado = RolEstado.Activo });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureBootstrapUsersAsync(ProyectoCafeDbContext db, CancellationToken ct)
    {
        if (_authOptions.Users.Count == 0) return;

        var rolesByName = await db.Roles
            .Where(r => r.Estado == RolEstado.Activo)
            .ToDictionaryAsync(r => r.Nombre, r => r, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var seed in _authOptions.Users)
        {
            var email = (seed.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) continue;

            var existing = await db.Usuarios
                .FirstOrDefaultAsync(u => u.EmailNormalizado == email && u.Estado != UsuarioEstado.Eliminado, ct);

            if (existing is null)
            {
                var passwordHash = !string.IsNullOrWhiteSpace(seed.PasswordHash)
                    ? seed.PasswordHash!
                    : PasswordHasher.HashPassword(seed.Password ?? string.Empty);

                existing = new Usuario
                {
                    Email = email,
                    EmailNormalizado = email,
                    Nombre = (seed.Name ?? email).Trim(),
                    PasswordHash = passwordHash,
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = DateTime.UtcNow,
                };

                db.Usuarios.Add(existing);
                await db.SaveChangesAsync(ct);
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

            var already = await db.UsuariosRoles
                .Where(ur => ur.UsuarioId == existing.UsuarioId)
                .Select(ur => ur.RolId)
                .ToListAsync(ct);

            foreach (var roleId in roleIds)
            {
                if (already.Contains(roleId)) continue;
                db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = existing.UsuarioId, RolId = roleId });
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task EnsureAdminPermissionsAsync(ProyectoCafeDbContext db, CancellationToken ct)
    {
        var roles = await db.Roles
            .Where(r => r.Estado == RolEstado.Activo)
            .ToListAsync(ct);

        var adminRole = roles.FirstOrDefault(r => string.Equals(r.Nombre, "Admin", StringComparison.OrdinalIgnoreCase));
        if (adminRole is null) return;

        var existing = await db.RolesPermisos
            .Where(rp => rp.RolId == adminRole.RolId)
            .Select(rp => new { rp.Modulo, rp.Accion })
            .ToListAsync(ct);

        foreach (var (modulo, accion) in Permissions.AllCrudPermissions())
        {
            var already = existing.Any(x =>
                string.Equals(x.Modulo, modulo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Accion, accion, StringComparison.OrdinalIgnoreCase));

            if (already) continue;

            db.RolesPermisos.Add(new RolPermiso
            {
                RolId = adminRole.RolId,
                Modulo = modulo,
                Accion = accion,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
