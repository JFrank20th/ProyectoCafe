using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProyectoCafe.API.Auth;
using ProyectoCafe.API.Endpoints;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class DashboardSummaryEndpointTests
{
    [Fact]
    public async Task GetSummary_RequiresAuthentication()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/v1/dashboard/summary?period=30d");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ReportsAdministrationMetricsForRequestedPeriod()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/summary?period=30d");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<DashboardEndpoints.DashboardSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal("30d", summary.Window.Period);
        Assert.NotNull(summary.Window.From);
        Assert.NotNull(summary.Window.To);

        // Usuarios no eliminados: 6 (el usuario eliminado lógicamente no cuenta).
        Assert.Equal(6, summary.Users.TotalCount);
        Assert.Equal(4, summary.Users.ActiveCount);
        Assert.Equal(1, summary.Users.PendingCount);
        Assert.Equal(1, summary.Users.InactiveCount);
        Assert.Equal(5, summary.Users.NewCount);

        // Roles no eliminados: 3, de los cuales 2 están activos.
        Assert.Equal(3, summary.Roles.TotalCount);
        Assert.Equal(2, summary.Roles.ActiveCount);
        Assert.Equal(3, summary.Roles.AssignmentsCount);

        // Permisos: 6 asignaciones, 4 combinaciones distintas en 3 módulos.
        Assert.Equal(6, summary.Permissions.AssignmentsCount);
        Assert.Equal(4, summary.Permissions.DistinctCount);
        Assert.Equal(3, summary.Permissions.ModuleCount);

        // Cobertura: 3 de 4 usuarios activos tienen rol vigente.
        Assert.Equal(4, summary.Access.ActiveUsersCount);
        Assert.Equal(3, summary.Access.UsersWithRolesCount);
        Assert.Equal(75m, summary.Access.CoveragePercent);

        Assert.NotNull(summary.PreviousPeriod);
        Assert.Equal("30d", summary.PreviousPeriod!.Window.Period);
        Assert.Equal(1, summary.PreviousPeriod.Users.NewCount);
        Assert.Equal(6, summary.PreviousPeriod.Users.TotalCount);
    }

    [Fact]
    public async Task GetSummary_AcceptsCustomRangeAndBuildsPreviousPeriodOfSameLength()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        var rangeStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-46)).ToString("yyyy-MM-dd");
        var rangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-40)).ToString("yyyy-MM-dd");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/dashboard/summary?period=custom&from={rangeStart}&to={rangeEnd}");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<DashboardEndpoints.DashboardSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal("custom", summary.Window.Period);
        Assert.Equal(rangeStart, summary.Window.From);
        Assert.Equal(rangeEnd, summary.Window.To);

        // Solo el usuario creado hace 45 días cae dentro del rango personalizado.
        Assert.Equal(1, summary.Users.NewCount);

        Assert.NotNull(summary.PreviousPeriod);
        Assert.Equal("custom", summary.PreviousPeriod!.Window.Period);
        Assert.Equal(0, summary.PreviousPeriod.Users.NewCount);
    }

    [Fact]
    public async Task GetSummary_ReturnsNoPreviousPeriodForAllTimeWindow()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/summary?period=all");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<DashboardEndpoints.DashboardSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal("all", summary.Window.Period);
        Assert.Null(summary.Window.From);
        Assert.Null(summary.Window.To);
        Assert.Null(summary.PreviousPeriod);
        Assert.Equal(6, summary.Users.NewCount);
    }

    [Fact]
    public async Task GetSummary_ReturnsValidationProblemForUnknownPeriod()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/summary?period=unsupported");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Periodo inválido", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSummary_ReturnsValidationProblemForIncompleteCustomRange()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/summary?period=custom&from=2026-03-01");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Debes enviar ambos parámetros from y to", body, StringComparison.Ordinal);
    }

    private sealed class DashboardApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"dashboard-tests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
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
                });
            });

            builder.ConfigureServices(services =>
            {
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                RemoveSecurityBootstrap(services);

                services.RemoveAll<DbContextOptions<ProyectoCafeDbContext>>();
                services.RemoveAll<ProyectoCafeDbContext>();

                services.AddDbContext<ProyectoCafeDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.UseInternalServiceProvider(inMemoryProvider);
                });

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build();

                    options.FallbackPolicy = options.DefaultPolicy;
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                Seed(db);
            });
        }

        private static void RemoveSecurityBootstrap(IServiceCollection services)
        {
            var descriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType == typeof(SecurityBootstrapHostedService))
                .ToArray();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        }

        private static void Seed(ProyectoCafeDbContext db)
        {
            var nowUtc = DateTime.UtcNow;

            db.Roles.AddRange(
                new Rol { RolId = 1, Nombre = "Admin", Estado = RolEstado.Activo },
                new Rol { RolId = 2, Nombre = "Operador", Estado = RolEstado.Activo },
                new Rol { RolId = 3, Nombre = "Auditor", Estado = RolEstado.Inactivo },
                new Rol { RolId = 4, Nombre = "Legacy", Estado = RolEstado.Eliminado });

            db.Usuarios.AddRange(
                new Usuario
                {
                    UsuarioId = 1,
                    Email = "activo.reciente@test.local",
                    EmailNormalizado = "activo.reciente@test.local",
                    Nombre = "Activo reciente",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = nowUtc.AddDays(-5),
                },
                new Usuario
                {
                    UsuarioId = 2,
                    Email = "activo.anterior@test.local",
                    EmailNormalizado = "activo.anterior@test.local",
                    Nombre = "Activo anterior",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = nowUtc.AddDays(-45),
                },
                new Usuario
                {
                    UsuarioId = 3,
                    Email = "pendiente@test.local",
                    EmailNormalizado = "pendiente@test.local",
                    Nombre = "Pendiente",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.PendienteActivacion,
                    CreadoEn = nowUtc.AddDays(-10),
                },
                new Usuario
                {
                    UsuarioId = 4,
                    Email = "inactivo@test.local",
                    EmailNormalizado = "inactivo@test.local",
                    Nombre = "Inactivo",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Inactivo,
                    CreadoEn = nowUtc.AddDays(-3),
                },
                new Usuario
                {
                    UsuarioId = 5,
                    Email = "eliminado@test.local",
                    EmailNormalizado = "eliminado@test.local",
                    Nombre = "Eliminado",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Eliminado,
                    CreadoEn = nowUtc.AddDays(-1),
                },
                new Usuario
                {
                    UsuarioId = 6,
                    Email = "activo.con.rol@test.local",
                    EmailNormalizado = "activo.con.rol@test.local",
                    Nombre = "Activo con rol",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = nowUtc.AddDays(-20),
                },
                new Usuario
                {
                    UsuarioId = 7,
                    Email = "activo.sin.rol@test.local",
                    EmailNormalizado = "activo.sin.rol@test.local",
                    Nombre = "Activo sin rol",
                    PasswordHash = "hash",
                    Estado = UsuarioEstado.Activo,
                    CreadoEn = nowUtc.AddDays(-15),
                });

            db.UsuariosRoles.AddRange(
                new UsuarioRol { UsuarioId = 1, RolId = 1 },
                new UsuarioRol { UsuarioId = 2, RolId = 1 },
                new UsuarioRol { UsuarioId = 6, RolId = 2 });

            db.RolesPermisos.AddRange(
                new RolPermiso { RolId = 1, Modulo = "Administracion.Usuarios", Accion = "Ver" },
                new RolPermiso { RolId = 1, Modulo = "Administracion.Usuarios", Accion = "Editar" },
                new RolPermiso { RolId = 1, Modulo = "Administracion.Roles", Accion = "Ver" },
                new RolPermiso { RolId = 2, Modulo = "Administracion.Usuarios", Accion = "Ver" },
                new RolPermiso { RolId = 2, Modulo = "Administracion.Permisos", Accion = "Ver" },
                new RolPermiso { RolId = 3, Modulo = "Administracion.Roles", Accion = "Ver" });

            db.SaveChanges();
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string UserHeaderName = "X-Test-User";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, headerValue.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim("uid", headerValue.ToString()),
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
