using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

namespace ProyectoCafe.Tests;

public sealed class AuthLoginEndpointTests
{
    private const string Password = "SuperSecret123!";

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndSetsAuthCookies()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedUserAsync("valid@test.local", Password);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "valid@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var joinedCookies = string.Join(';', cookies!);
        Assert.Contains("pc_at=", joinedCookies);
        Assert.Contains("pc_csrf=", joinedCookies);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("valid@test.local", body.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccountForFifteenMinutes()
    {
        await using var factory = new AuthApiFactory();
        var userId = await factory.SeedUserAsync("locked@test.local", Password);
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = "locked@test.local",
                password = "wrong-password",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);

            using var failedBody = JsonDocument.Parse(await failed.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_CREDENTIALS", failedBody.RootElement.GetProperty("error").GetProperty("code").GetString());
        }

        // El sexto intento, incluso con la contraseña correcta, debe respetar el bloqueo persistente.
        var locked = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "locked@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.NotNull(locked.Headers.RetryAfter);
        Assert.True(locked.Headers.RetryAfter!.Delta > TimeSpan.Zero);

        using var lockedBody = JsonDocument.Parse(await locked.Content.ReadAsStringAsync());
        var error = lockedBody.RootElement.GetProperty("error");
        Assert.Equal("TOO_MANY_REQUESTS", error.GetProperty("code").GetString());
        Assert.Equal("Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.", error.GetProperty("message").GetString());

        var user = await factory.GetUserAsync(userId);
        Assert.Equal(5, user.IntentosFallidosLogin);
        Assert.NotNull(user.LockoutFin);
        Assert.InRange(user.LockoutFin!.Value, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task Login_RateLimitPerIp_BlocksAfterConfiguredFailures()
    {
        await using var factory = new AuthApiFactory(new Dictionary<string, string?>
        {
            ["Auth:RateLimit:IpMaxFailedAttempts"] = "2",
            ["Auth:RateLimit:IpWindowMinutes"] = "5",
            ["Auth:RateLimit:UserMaxFailedAttempts"] = "100",
            ["Auth:RateLimit:UserWindowMinutes"] = "15",
        });
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = $"unknown-{attempt}@test.local",
                password = "wrong-password",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "unknown-3@test.local",
            password = "wrong-password",
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        Assert.True(blocked.Headers.RetryAfter!.Delta > TimeSpan.Zero);

        using var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("TOO_MANY_REQUESTS", error.GetProperty("code").GetString());
        Assert.Equal("Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_FailedAttempt_LogsSuspiciousAttemptWithoutPassword()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedUserAsync("victim@test.local", Password);
        using var client = factory.CreateClient();

        var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "victim@test.local",
            password = "wrong-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);

        var warning = factory.Logs.Entries.FirstOrDefault(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Category == "ProyectoCafe.API.Auth.Login" &&
            entry.Message.Contains("Login fallido", StringComparison.Ordinal));

        Assert.NotNull(warning);
        Assert.Contains("v***@test.local", warning!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong-password", warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, warning.Message, StringComparison.Ordinal);
    }

    private sealed class AuthApiFactory(IReadOnlyDictionary<string, string?>? overrides = null)
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"auth-tests-{Guid.NewGuid():N}";
        private readonly IReadOnlyDictionary<string, string?> _overrides = overrides ?? new Dictionary<string, string?>();

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
                    ["Auth:Jwt:SigningKey"] = "tests-signing-key-with-32-bytes-minimum!!",
                    ["Auth:Jwt:AccessTokenLifetimeMinutes"] = "60",
                    ["Auth:Cookie:AccessTokenCookieName"] = "pc_at",
                    ["Auth:Cookie:CsrfCookieName"] = "pc_csrf",
                    ["Auth:Cookie:CsrfHeaderName"] = "X-CSRF",
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
                var hostedServices = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                        && descriptor.ImplementationType == typeof(SecurityBootstrapHostedService))
                    .ToArray();

                foreach (var descriptor in hostedServices)
                {
                    services.Remove(descriptor);
                }

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

        public async Task<int> SeedUserAsync(string email, string password)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

            var user = new Usuario
            {
                Email = email,
                EmailNormalizado = email.ToLowerInvariant(),
                Nombre = "Test User",
                PasswordHash = PasswordHasher.HashPassword(password, iterations: 10_000),
                Estado = UsuarioEstado.Activo,
                CreadoEn = DateTime.UtcNow,
            };

            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            return user.UsuarioId;
        }

        public async Task<Usuario> GetUserAsync(int userId)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
            return await db.Usuarios.AsNoTracking().SingleAsync(u => u.UsuarioId == userId);
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
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
