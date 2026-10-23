using Microsoft.Extensions.Options;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Abstractions.Security;

namespace ProyectoCafe.API.Auth;

public sealed class SecurityBootstrapHostedService(
    IServiceProvider serviceProvider,
    IOptions<AuthOptions> authOptions,
    IHostEnvironment environment,
    IPermissionCatalog permissionCatalog,
    ILogger<SecurityBootstrapHostedService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly IPermissionCatalog _permissionCatalog = permissionCatalog;
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
            var store = scope.ServiceProvider.GetRequiredService<ISecurityBootstrapStore>();

            if (_environment.IsDevelopment())
            {
                await store.MigrateAsync(cancellationToken);
            }
            else
            {
                // En producción no se siembran cuentas de prueba, pero una base
                // de datos existente podría conservar admin@local/admin o
                // user@local/user. Detectarlo aborta el arranque.
                await EnsureNoDefaultCredentialsAsync(store, cancellationToken);
            }

            await EnsureBootstrapRolesAsync(store, cancellationToken);

            // Los usuarios de prueba (admin@local/user@local) solo se siembran
            // en Development. Nunca se crean credenciales conocidas en otros
            // entornos.
            if (_environment.IsDevelopment())
            {
                await EnsureBootstrapUsersAsync(store, cancellationToken);
            }

            await EnsureAdminPermissionsAsync(store, cancellationToken);
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

    private static async Task EnsureNoDefaultCredentialsAsync(
        ISecurityBootstrapStore store,
        CancellationToken ct)
    {
        var candidates = await store.GetDefaultCredentialCandidatesAsync(ct);

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

    private async Task EnsureBootstrapRolesAsync(ISecurityBootstrapStore store, CancellationToken ct)
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

        await store.EnsureRolesAsync(roleNames, ct);
    }

    private async Task EnsureBootstrapUsersAsync(ISecurityBootstrapStore store, CancellationToken ct)
    {
        var users = _authOptions.Users
            .Select(seed => new SecurityBootstrapUser(
                seed.Email,
                seed.Name,
                seed.PasswordHash,
                seed.Roles))
            .ToArray();

        await store.EnsureUsersAsync(users, ct);
    }

    private async Task EnsureAdminPermissionsAsync(ISecurityBootstrapStore store, CancellationToken ct)
    {
        // El rol Admin recibe todos los permisos que la API expone en este momento.
        // Si el catálogo aún no está disponible, se usa el catálogo CRUD base como respaldo.
        var catalogPermissions = _permissionCatalog.GetCatalog()
            .SelectMany(item => item.Acciones.Select(accion => new SecurityPermission(item.Modulo, accion)))
            .ToArray();

        var permissions = catalogPermissions.Length > 0
            ? catalogPermissions
            : Permissions.AllCrudPermissions()
                .Select(p => new SecurityPermission(p.Modulo, p.Accion))
                .ToArray();

        await store.EnsureRolePermissionsAsync("Admin", permissions, ct);
    }
}
