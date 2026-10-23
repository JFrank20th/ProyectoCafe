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
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class PermissionCatalogEndpointTests
{
    [Fact]
    public async Task GetCatalog_ReflectsOnlyActionsExposedByEndpoints()
    {
        await using var factory = new CatalogApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/permissions/catalog");
        request.Headers.Add(TestAuthHandler.UserHeaderName, "1");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var catalog = await response.Content.ReadFromJsonAsync<CatalogItem[]>();
        Assert.NotNull(catalog);

        var byModule = catalog!
            .ToDictionary(item => item.Modulo, item => item.Acciones, StringComparer.OrdinalIgnoreCase);

        // El alcance actual del proyecto solo expone los módulos de Administración.
        Assert.Equal(3, catalog!.Length);
        Assert.Equal(
            new[]
            {
                "Administracion.Permisos",
                "Administracion.Roles",
                "Administracion.Usuarios",
            },
            catalog.Select(item => item.Modulo).ToArray());

        // Usuarios se activa/desactiva y se le asignan roles, pero no se crea desde administración.
        Assert.Equal(new[] { "Ver", "Editar", "Eliminar" }, byModule["Administracion.Usuarios"]);

        // Roles permite el ciclo CRUD completo.
        Assert.Equal(new[] { "Ver", "Crear", "Editar", "Eliminar" }, byModule["Administracion.Roles"]);

        // El propio módulo de permisos se descubre por sus endpoints de consulta y edición.
        Assert.Equal(new[] { "Ver", "Editar" }, byModule["Administracion.Permisos"]);
    }

    private sealed record CatalogItem(string Modulo, string[] Acciones);

    private sealed class CatalogApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"permission-catalog-tests-{Guid.NewGuid():N}";

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

                var hostedServices = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                        && descriptor.ImplementationType == typeof(SecurityBootstrapHostedService))
                    .ToArray();

                foreach (var descriptor in hostedServices)
                {
                    services.Remove(descriptor);
                }

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

                services.AddSingleton<IAuthorizationHandler, AllowAllPermissionsHandler>();
            });
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

    private sealed class AllowAllPermissionsHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
