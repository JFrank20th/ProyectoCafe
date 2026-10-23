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
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class ProductosEndpointTests
{
    private const string TestCsrfToken = "test-csrf-token";

    [Fact]
    public async Task Post_ProductWithoutPresentation_CreatesProduct()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/productos", new
        {
            nombre = "Cafe sin categoria",
            descripcion = "Producto flexible",
            precio = 7m,
            precioInternet = 7m,
            precioMayorista = 7m,
            estado = true,
            unidadMedida = "G",
            preciosTipoVenta = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var producto = await db.Productos.SingleAsync(x => x.Nombre == "Cafe sin categoria");

        Assert.Null(producto.PresentacionNombre);
    }

    [Fact]
    public async Task Post_ProductWithoutUnit_ReturnsValidationProblem()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/productos", new
        {
            nombre = "Cafe molido",
            descripcion = "Sin unidad",
            precio = 12m,
            precioInternet = 12m,
            precioMayorista = 12m,
            estado = true,
            preciosTipoVenta = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("UnidadMedida es requerida", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_ProductWithInactiveUnit_ReturnsValidationProblem()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
            db.UnidadesMedida.Add(new UnidadMedida { Codigo = "PAQ", Nombre = "Paquete", Estado = false });
            db.SaveChanges();
        }

        var response = await client.PostAsJsonAsync("/api/v1/productos", new
        {
            nombre = "Cafe empacado",
            descripcion = "Unidad inactiva",
            precio = 17m,
            precioInternet = 17m,
            precioMayorista = 17m,
            estado = true,
            unidadMedida = "PAQ",
            preciosTipoVenta = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("unidad de medida", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inactiva", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Put_ProductClearsLegacyPresentation_WhenUpdatingUnitOnly()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
            db.Productos.Add(new Producto
            {
                ProductoId = 3,
                Nombre = "Cafe especial",
                Descripcion = "Con formato legacy",
                Precio = 15m,
                PrecioInternet = 15m,
                PrecioMayorista = 15m,
                Stock = 0m,
                ManejaLotes = true,
                TipoLote = "Manual",
                Estado = true,
                UnidadMedida = "KG",
                PresentacionCodigo = "BOLSA-HOGAR-250",
                PresentacionNombre = "Bolsa hogar",
                PresentacionCantidad = 250m,
                PresentacionUnidadMedida = "G",
                PermiteDecimal = true,
            });
            db.SaveChanges();
        }

        var response = await client.PutAsJsonAsync("/api/v1/productos/3", new
        {
            nombre = "Cafe especial",
            descripcion = "Con formato legacy",
            precio = 17m,
            precioInternet = 17m,
            precioMayorista = 17m,
            estado = true,
            unidadMedida = "KG",
            preciosTipoVenta = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var validationScope = factory.Services.CreateScope();
        var validationDb = validationScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var producto = await validationDb.Productos.SingleAsync(x => x.ProductoId == 3);

        Assert.Null(producto.PresentacionNombre);
        Assert.Null(producto.PresentacionCodigo);
        Assert.Null(producto.PresentacionCantidad);
        Assert.Null(producto.PresentacionUnidadMedida);
        Assert.Equal(17m, producto.Precio);
    }

    [Fact]
    public async Task Delete_ProductWithOperationalUsage_ReturnsValidationProblem()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.DeleteAsync("/api/v1/productos/1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("uso operacional", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_ProductWithoutOperationalUsage_RemovesProduct()
    {
        await using var factory = new ProductosApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.DeleteAsync("/api/v1/productos/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var exists = await db.Productos.AnyAsync(x => x.ProductoId == 2);

        Assert.False(exists);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, "1");
        client.DefaultRequestHeaders.Add("X-CSRF", TestCsrfToken);
        client.DefaultRequestHeaders.Add("Cookie", $"pc_csrf={TestCsrfToken}");
        return client;
    }

    private sealed class ProductosApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"productos-tests-{Guid.NewGuid():N}";

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
                    Nombre = "Cafe pergamino",
                    Descripcion = "Con uso",
                    Precio = 10m,
                    PrecioInternet = 10m,
                    PrecioMayorista = 10m,
                    Stock = 5m,
                    ManejaLotes = true,
                    TipoLote = "Manual",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                },
                new Producto
                {
                    ProductoId = 2,
                    Nombre = "Cafe molido",
                    Descripcion = "Sin uso",
                    Precio = 8m,
                    PrecioInternet = 8m,
                    PrecioMayorista = 8m,
                    Stock = 0m,
                    ManejaLotes = true,
                    TipoLote = "Manual",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                });

            db.Lotes.Add(new Lote
            {
                LoteId = 1,
                ProductoId = 1,
                CodigoLote = "LOT-USED-001",
                CantidadInicial = 5m,
                CantidadDisponible = 5m,
                Estado = "Activo",
                FechaCreacion = DateTime.UtcNow.AddDays(-1),
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