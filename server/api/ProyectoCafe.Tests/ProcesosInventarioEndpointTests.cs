using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Lotes;
using ProyectoCafe.Application.Inventario.ProcesosInventario;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class ProcesosInventarioEndpointTests
{
    private const string TestCsrfToken = "test-csrf-token";

    [Fact]
    public async Task Create_PartialProcess_ConsumesOriginCreatesOutputLotAndCost()
    {
        await using var factory = new ProcesosInventarioApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/procesos-inventario", new
        {
            recepcionCafeId = 1,
            productoSalidaId = 2,
            usaLoteCompleto = false,
            cantidadOrigenConsumida = 4.5m,
            cantidadSalida = 3.75m,
            costoProceso = 12m,
            observaciones = "Proceso inicial",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProcesoInventarioCreateResult>();

        Assert.NotNull(result);
        Assert.Equal(4.5m, result.CantidadOrigenConsumida);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var proceso = await db.ProcesosInventario.SingleAsync();
        var recepcion = await db.RecepcionesCafe.SingleAsync(x => x.RecepcionCafeId == 1);
        var loteSalida = await db.Lotes.SingleAsync(x => x.LoteId == result.LoteSalidaId);
        var productoSalida = await db.Productos.SingleAsync(x => x.ProductoId == 2);
        var movimientos = await db.LoteMovimientos.OrderBy(x => x.MovimientoId).ToListAsync();
        var costo = await db.Costos.SingleAsync(x => x.FuenteModulo == "Inventario.ProcesosInventario" && x.ReferenciaId == proceso.ProcesoInventarioId);

        Assert.Equal(1, proceso.RecepcionCafeId);
        Assert.Null(proceso.LoteOrigenId);
        Assert.Equal(2, proceso.ProductoSalidaId);
        Assert.Equal(4.5m, proceso.CantidadOrigenConsumida);
        Assert.Equal(3.75m, proceso.CantidadSalida);
        Assert.Equal("Registrado", proceso.Estado);
        Assert.Equal(3.5m, recepcion.CantidadDisponible);
        Assert.Equal(3.75m, loteSalida.CantidadInicial);
        Assert.Equal(3.75m, loteSalida.CantidadDisponible);
        Assert.StartsWith("PRC-002-", loteSalida.CodigoLote, StringComparison.Ordinal);
        Assert.Equal(3.75m, productoSalida.Stock);
        Assert.Single(movimientos);
        Assert.Equal("ProcesoEntrada", movimientos[0].Tipo);
        Assert.Equal(12m, costo.Monto);
        Assert.Equal(true, costo.Estado);
    }

    [Fact]
    public async Task Create_CompleteProcess_UsesEntireAvailableQuantity()
    {
        await using var factory = new ProcesosInventarioApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/procesos-inventario", new
        {
            recepcionCafeId = 1,
            productoSalidaId = 2,
            usaLoteCompleto = true,
            cantidadSalida = 6m,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProcesoInventarioCreateResult>();

        Assert.NotNull(result);
        Assert.Equal(8m, result.CantidadOrigenConsumida);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var proceso = await db.ProcesosInventario.SingleAsync();
        var recepcion = await db.RecepcionesCafe.SingleAsync(x => x.RecepcionCafeId == 1);

        Assert.True(proceso.UsaLoteCompleto);
        Assert.Equal(8m, proceso.CantidadOrigenConsumida);
        Assert.Equal(0m, recepcion.CantidadDisponible);
    }

    [Fact]
    public async Task Cancel_Process_RevertsInventoryAndDisablesCost()
    {
        await using var factory = new ProcesosInventarioApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var createResponse = await client.PostAsJsonAsync("/api/v1/procesos-inventario", new
        {
            recepcionCafeId = 1,
            productoSalidaId = 2,
            usaLoteCompleto = false,
            cantidadOrigenConsumida = 4m,
            cantidadSalida = 3m,
            costoProceso = 9m,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ProcesoInventarioCreateResult>();

        Assert.NotNull(createResult);

        var cancelResponse = await client.DeleteAsync($"/api/v1/procesos-inventario/{createResult.ProcesoInventarioId}");

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var proceso = await db.ProcesosInventario.SingleAsync(x => x.ProcesoInventarioId == createResult.ProcesoInventarioId);
        var recepcion = await db.RecepcionesCafe.SingleAsync(x => x.RecepcionCafeId == 1);
        var loteSalida = await db.Lotes.SingleAsync(x => x.LoteId == createResult.LoteSalidaId);
        var productoSalida = await db.Productos.SingleAsync(x => x.ProductoId == 2);
        var costo = await db.Costos.SingleAsync(x => x.FuenteModulo == "Inventario.ProcesosInventario" && x.ReferenciaId == createResult.ProcesoInventarioId);

        Assert.Equal("Anulado", proceso.Estado);
        Assert.Equal(8m, recepcion.CantidadDisponible);
        Assert.Equal(0m, loteSalida.CantidadDisponible);
        Assert.Equal("Inactivo", loteSalida.Estado);
        Assert.Equal(0m, productoSalida.Stock);
        Assert.Equal(false, costo.Estado);
    }

    [Fact]
    public async Task Create_Process_UpdatesLotTraceabilityForOriginAndOutput()
    {
        await using var factory = new ProcesosInventarioApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var createResponse = await client.PostAsJsonAsync("/api/v1/procesos-inventario", new
        {
            recepcionCafeId = 1,
            productoSalidaId = 2,
            usaLoteCompleto = false,
            cantidadOrigenConsumida = 4m,
            cantidadSalida = 3m,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ProcesoInventarioCreateResult>();

        Assert.NotNull(createResult);

        var outputTraceability = await client.GetFromJsonAsync<LoteTraceabilityResponse>($"/api/v1/lotes/{createResult.LoteSalidaId}/trazabilidad");

        Assert.NotNull(outputTraceability);
        Assert.Equal("ProcesoInventario", outputTraceability.Origen?.Tipo);
        Assert.Equal("REC-ORIGEN-001", outputTraceability.Origen?.CodigoLoteOrigen);
        Assert.Single(outputTraceability.ProcesosRelacionados);
        Assert.Equal("Salida", outputTraceability.ProcesosRelacionados[0].Rol);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, "1");
        client.DefaultRequestHeaders.Add("X-CSRF", TestCsrfToken);
        client.DefaultRequestHeaders.Add("Cookie", $"pc_csrf={TestCsrfToken}");
        return client;
    }

    private sealed class ProcesosInventarioApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"procesos-inventario-tests-{Guid.NewGuid():N}";

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
                    options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
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
                    Precio = 0m,
                    PrecioInternet = 0m,
                    PrecioMayorista = 0m,
                    Stock = 8m,
                    ManejaLotes = true,
                    TipoLote = "Cafe",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                },
                new Producto
                {
                    ProductoId = 2,
                    Nombre = "Cafe tostado",
                    Precio = 0m,
                    PrecioInternet = 0m,
                    PrecioMayorista = 0m,
                    Stock = 0m,
                    ManejaLotes = true,
                    TipoLote = "Cafe",
                    Estado = true,
                    UnidadMedida = "KG",
                    PermiteDecimal = true,
                });

            db.RecepcionesCafe.Add(new RecepcionCafe
            {
                RecepcionCafeId = 1,
                ProveedorId = 1,
                ProductoId = 1,
                TipoCafe = "Oreado",
                FechaRecepcion = DateTime.UtcNow.AddDays(-1),
                CodigoLote = "REC-ORIGEN-001",
                Cantidad = 10m,
                CantidadDisponible = 8m,
                PrecioUnitario = 2m,
                Total = 20m,
                Estado = "Registrada",
            });

            db.Proveedores.Add(new Proveedor
            {
                ProveedorId = 1,
                Nombre = "Proveedor origen",
                Estado = true,
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