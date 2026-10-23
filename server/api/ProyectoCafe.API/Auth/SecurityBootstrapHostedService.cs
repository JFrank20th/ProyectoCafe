using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Auth;

public sealed class SecurityBootstrapHostedService(
    IServiceProvider serviceProvider,
    IOptions<AuthOptions> authOptions,
    IHostEnvironment environment,
    PermissionCatalogService permissionCatalog,
    ILogger<SecurityBootstrapHostedService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly PermissionCatalogService _permissionCatalog = permissionCatalog;
    private readonly ILogger<SecurityBootstrapHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Defensa en profundidad: el validador de opciones ya rechaza usuarios
        // iniciales fuera de Development. Si por cualquier motivo se omitiera,
        // el arranque falla en lugar de sembrar cuentas de prueba en producción.
        if (!_environment.IsDevelopment() && _authOptions.Users.Count > 0)
        {
            throw new InvalidOperationException(
                "Auth:Users solo está permitido en Development. " +
                "Elimina los usuarios de prueba de la configuración antes de iniciar en este entorno.");
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

            if (_environment.IsDevelopment())
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                // En producción no se siembran cuentas de prueba, pero una base
                // de datos existente podría conservar admin@local/admin o
                // user@local/user. Detectarlo aborta el arranque.
                await EnsureNoDefaultCredentialsAsync(db, cancellationToken);
            }

            await EnsureBootstrapRolesAsync(db, cancellationToken);

            // Los usuarios de prueba (admin@local/user@local) solo se siembran
            // en Development. Nunca se crean credenciales conocidas en otros
            // entornos.
            if (_environment.IsDevelopment())
            {
                await EnsureBootstrapUsersAsync(db, cancellationToken);
            }

            await EnsureAdminPermissionsAsync(db, cancellationToken);
        }
        catch (InsecureConfigurationException ex)
        {
            _logger.LogCritical(ex, "Security bootstrap detectó credenciales inseguras. Se aborta el arranque.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security bootstrap no pudo ejecutarse. La API seguirá iniciando, pero auth/admin puede fallar hasta que la BD esté lista.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureNoDefaultCredentialsAsync(ProyectoCafeDbContext db, CancellationToken ct)
    {
        var candidates = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.Estado != UsuarioEstado.Eliminado &&
                        (u.EmailNormalizado == "admin@local" || u.EmailNormalizado == "user@local"))
            .Select(u => new { u.EmailNormalizado, u.PasswordHash })
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            if (!SecurityDefaults.IsKnownDefaultCredential(candidate.EmailNormalizado, candidate.PasswordHash))
            {
                continue;
            }

            throw new InsecureConfigurationException(
                $"Se detectó la cuenta de prueba '{candidate.EmailNormalizado}' con una contraseña predeterminada. " +
                "Cambia su contraseña o elimina la cuenta antes de iniciar en producción.");
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

            // El validador garantiza el formato; este chequeo evita que un
            // valor inválido llegue a la base de datos si se omite la validación.
            if (!PasswordHasher.TryParse(seed.PasswordHash, out _, out _, out _))
            {
                throw new InvalidOperationException(
                    $"Auth:Users[{email}]: PasswordHash no es válido. No se permiten contraseñas en texto plano.");
            }

            var existing = await db.Usuarios
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

                db.Usuarios.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            else if (!string.Equals(existing.PasswordHash, seed.PasswordHash.Trim(), StringComparison.Ordinal))
            {
                // Mantiene sincronizadas las credenciales de desarrollo cuando
                // se rota el hash en appsettings.Development.json.
                existing.PasswordHash = seed.PasswordHash.Trim();
                existing.ActualizadoEn = DateTime.UtcNow;
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

    private async Task EnsureAdminPermissionsAsync(ProyectoCafeDbContext db, CancellationToken ct)
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

        // El rol Admin recibe todos los permisos que la API expone en este momento.
        // Si el catálogo aún no está disponible, se usa el catálogo CRUD base como respaldo.
        var catalogPermissions = _permissionCatalog.GetCatalog()
            .SelectMany(item => item.Acciones.Select(accion => (Modulo: item.Modulo, Accion: accion)))
            .ToArray();

        var permissions = catalogPermissions.Length > 0
            ? catalogPermissions
            : Permissions.AllCrudPermissions().ToArray();

        foreach (var (modulo, accion) in permissions)
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
