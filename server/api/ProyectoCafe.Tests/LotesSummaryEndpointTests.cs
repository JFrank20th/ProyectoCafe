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
using ProyectoCafe.Application.Inventario.Lotes;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class LotesSummaryEndpointTests
{
    [Fact]
    public async Task GetSummary_RequiresAuthentication()
    {
        await using var factory = new LotesApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/v1/lotes/resumen?soloActivos=true");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_GroupsLotsByProductAndAppliesProductFilter()
    {
        await using var factory = new LotesApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, "1");

        var summary = await client.GetFromJsonAsync<List<LoteExistenciaResumenResponse>>("/api/v1/lotes/resumen?soloActivos=true");
        var filtered = await client.GetFromJsonAsync<List<LoteExistenciaResumenResponse>>("/api/v1/lotes/resumen?soloActivos=true&productoId=1");

        Assert.NotNull(summary);
        Assert.NotNull(filtered);
        Assert.Equal(2, summary.Count);

        var cafeMolido = summary.Single(x => x.ProductoId == 1);
        Assert.Equal("Cafe molido", cafeMolido.ProductoNombre);
        Assert.Equal(2, cafeMolido.Lotes);
        Assert.Equal(2, cafeMolido.LotesActivos);
        Assert.Equal(1, cafeMolido.LotesSinStock);
        Assert.Equal(15m, cafeMolido.CantidadInicial);
        Assert.Equal(6m, cafeMolido.CantidadDisponible);

        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].ProductoId);
        Assert.Equal(6m, filtered[0].CantidadDisponible);
    }

    private sealed class LotesApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"lotes-summary-tests-{Guid.NewGuid():N}";

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

                services.AddSingleton<IAuthorizationHandler, AllowAllPermissionsHandler>();

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
            db.Productos.AddRange(
                new Producto
                {
                    ProductoId = 1,
                    Nombre = "Cafe molido",
                    Precio = 0m,
                    PrecioInternet = 0m,
                    PrecioMayorista = 0m,
                    Stock = 6m,
                    ManejaLotes = true,
                    TipoLote = "Cafe",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                },
                new Producto
                {
                    ProductoId = 2,
                    Nombre = "Cafe en grano",
                    Precio = 0m,
                    PrecioInternet = 0m,
                    PrecioMayorista = 0m,
                    Stock = 2m,
                    ManejaLotes = true,
                    TipoLote = "Cafe",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                });

            db.Lotes.AddRange(
                new Lote
                {
                    LoteId = 1,
                    ProductoId = 1,
                    CodigoLote = "LOT-001",
                    CantidadInicial = 10m,
                    CantidadDisponible = 6m,
                    Estado = "Activo",
                    FechaCreacion = DateTime.UtcNow.AddDays(-3),
                },
                new Lote
                {
                    LoteId = 2,
                    ProductoId = 1,
                    CodigoLote = "LOT-002",
                    CantidadInicial = 5m,
                    CantidadDisponible = 0m,
                    Estado = "Activo",
                    FechaCreacion = DateTime.UtcNow.AddDays(-1),
                },
                new Lote
                {
                    LoteId = 3,
                    ProductoId = 2,
                    CodigoLote = "LOT-003",
                    CantidadInicial = 4m,
                    CantidadDisponible = 2m,
                    Estado = "Activo",
                    FechaCreacion = DateTime.UtcNow.AddDays(-2),
                },
                new Lote
                {
                    LoteId = 4,
                    ProductoId = 2,
                    CodigoLote = "LOT-004",
                    CantidadInicial = 3m,
                    CantidadDisponible = 3m,
                    Estado = "Inactivo",
                    FechaCreacion = DateTime.UtcNow.AddDays(-4),
                });

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

    private sealed class AllowAllPermissionsHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}