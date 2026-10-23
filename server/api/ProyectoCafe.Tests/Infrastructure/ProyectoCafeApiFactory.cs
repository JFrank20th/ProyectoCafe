using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests.Infrastructure;

/// <summary>
/// Factory compartida por las pruebas de integración. Cada instancia usa una base de datos
/// en memoria aislada para que las pruebas puedan ejecutarse en paralelo sin compartir estado.
/// </summary>
internal sealed class ProyectoCafeApiFactory : WebApplicationFactory<Program>
{
    public const string DefaultPassword = "SuperSecret123!";
    private const string DefaultSigningKey = "tests-signing-key-with-32-bytes-minimum!!";

    private readonly string _databaseName = $"proyecto-cafe-tests-{Guid.NewGuid():N}";
    private readonly IReadOnlyDictionary<string, string?> _overrides;

    public ProyectoCafeApiFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
    }

    public CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:3010",
                ["Database:ConnectionString"] = "Server=(localdb)\\mssqllocaldb;Database=ProyectoCafeTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Auth:Jwt:Issuer"] = "tests",
                ["Auth:Jwt:Audience"] = "tests",
                ["Auth:Jwt:SigningKey"] = DefaultSigningKey,
                ["Auth:Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Auth:Cookie:AccessTokenCookieName"] = "pc_at",
                ["Auth:Cookie:CsrfCookieName"] = "pc_csrf",
                ["Auth:Cookie:CsrfHeaderName"] = "X-CSRF",
                // Las pruebas de autenticación que necesitan el limitador lo habilitan explícitamente.
                ["Auth:RateLimit:Enabled"] = "false",
            };

            foreach (var (key, value) in _overrides)
            {
                settings[key] = value;
            }

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));

        builder.ConfigureServices(services =>
        {
            RemoveHostedService<SecurityBootstrapHostedService>(services);

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.RemoveAll<DbContextOptions<ProyectoCafeDbContext>>();
            services.RemoveAll<ProyectoCafeDbContext>();

            services.AddDbContext<ProyectoCafeDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(inMemoryProvider);
            });
        });
    }

    public async Task<int> SeedUserAsync(
        string email,
        UsuarioEstado state = UsuarioEstado.Activo,
        string? password = null,
        params string[] roles)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new Usuario
        {
            Email = normalizedEmail,
            EmailNormalizado = normalizedEmail,
            Nombre = $"Test {normalizedEmail}",
            PasswordHash = PasswordHasher.HashPassword(password ?? DefaultPassword, iterations: 10_000),
            Estado = state,
            CreadoEn = DateTime.UtcNow,
        };

        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        foreach (var roleName in roles.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var role = await db.Roles.SingleOrDefaultAsync(r => r.Nombre == roleName.Trim());
            if (role is null)
            {
                role = new Rol { Nombre = roleName.Trim(), Estado = RolEstado.Activo };
                db.Roles.Add(role);
                await db.SaveChangesAsync();
            }

            db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = user.UsuarioId, RolId = role.RolId });
        }

        await db.SaveChangesAsync();
        return user.UsuarioId;
    }

    public async Task<int> SeedUserWithPermissionsAsync(
        string email,
        params (string Modulo, string Accion)[] permissions)
    {
        var roleName = $"Role-{Guid.NewGuid():N}";
        await SeedRoleAsync(roleName, RolEstado.Activo, permissions);
        return await SeedUserAsync(email, roles: roleName);
    }

    public async Task<int> SeedRoleAsync(
        string name,
        RolEstado state = RolEstado.Activo,
        params (string Modulo, string Accion)[] permissions)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Nombre == name);
        if (role is null)
        {
            role = new Rol { Nombre = name, Estado = state };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        foreach (var (modulo, accion) in permissions)
        {
            db.RolesPermisos.Add(new RolPermiso
            {
                RolId = role.RolId,
                Modulo = modulo,
                Accion = accion,
            });
        }

        await db.SaveChangesAsync();
        return role.RolId;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        string? password = null,
        string? csrfTokenOverride = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = password ?? DefaultPassword,
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"No se pudo autenticar a '{email}'. HTTP {(int)response.StatusCode}: {body}");
        }

        var csrfToken = csrfTokenOverride ?? ReadCookie(response, "pc_csrf");
        if (!string.IsNullOrWhiteSpace(csrfToken))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-CSRF", csrfToken);
        }

        return client;
    }

    public async Task<Usuario> GetUserAsync(int userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        return await db.Usuarios.AsNoTracking().SingleAsync(u => u.UsuarioId == userId);
    }

    public async Task SetUserStateAsync(int userId, UsuarioEstado state)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var user = await db.Usuarios.SingleAsync(u => u.UsuarioId == userId);
        user.Estado = state;
        await db.SaveChangesAsync();
    }

    public async Task<Usuario?> FindUserByEmailAsync(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.EmailNormalizado == normalizedEmail);
    }

    public async Task<Rol> GetRoleAsync(int roleId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        return await db.Roles.AsNoTracking().SingleAsync(r => r.RolId == roleId);
    }

    public async Task<string[]> GetUserRolesAsync(int userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        return await db.UsuariosRoles
            .Where(ur => ur.UsuarioId == userId)
            .Join(db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r.Nombre)
            .OrderBy(name => name)
            .ToArrayAsync();
    }

    public async Task<(string Modulo, string Accion)[]> GetRolePermissionsAsync(int roleId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        return await db.RolesPermisos
            .Where(rp => rp.RolId == roleId)
            .OrderBy(rp => rp.Modulo)
            .ThenBy(rp => rp.Accion)
            .Select(rp => new ValueTuple<string, string>(rp.Modulo, rp.Accion))
            .ToArrayAsync();
    }

    public static string ReadCookie(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            return string.Empty;
        }

        foreach (var header in headers)
        {
            var parts = header.Split(';', 2);
            var index = parts[0].IndexOf('=', StringComparison.Ordinal);
            if (index <= 0)
            {
                continue;
            }

            if (string.Equals(parts[0][..index], cookieName, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(parts[0][(index + 1)..]);
            }
        }

        return string.Empty;
    }

    private static void RemoveHostedService<TService>(IServiceCollection services)
        where TService : class, IHostedService
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TService))
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<CapturedLog> _entries = new();

        public IReadOnlyCollection<CapturedLog> Entries => _entries;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        public sealed record CapturedLog(string Category, LogLevel Level, string Message);

        private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Enqueue(new CapturedLog(category, logLevel, formatter(state, exception)));
            }
        }
    }
}
