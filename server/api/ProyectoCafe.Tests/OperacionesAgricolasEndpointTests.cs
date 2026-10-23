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
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class OperacionesAgricolasEndpointTests
{
    private const string TestCsrfToken = "test-csrf-token";

    [Fact]
    public async Task Create_SupplyOperationMissingVisibleRequiredFields_ReturnsValidationProblem()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/produccion/operaciones-agricolas", new
        {
            cultivoId = 1,
            tipoOperacion = "Fertilizacion",
            costoManoObra = 3m,
            costoOtros = 1m,
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("FechaOperacion es requerida", body, StringComparison.Ordinal);
        Assert.Contains("Observaciones es requerida", body, StringComparison.Ordinal);
        Assert.Contains("InsumoId es requerido", body, StringComparison.Ordinal);
        Assert.Contains("CantidadInsumo debe ser mayor a 0", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_SupplyOperation_RebalancesInventoryAndCosts()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var fecha = DateTime.UtcNow.Date.AddDays(-1);
        var response = await client.PutAsJsonAsync("/api/v1/produccion/operaciones-agricolas/1", new
        {
            cultivoId = 1,
            tipoOperacion = "AplicacionInsumo",
            fechaOperacion = fecha,
            insumoId = 2,
            cantidadInsumo = 3m,
            costoManoObra = 7m,
            costoOtros = 1m,
            observaciones = "Aplicacion ajustada",
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var operacion = await db.OperacionesAgricolas.SingleAsync(x => x.OperacionAgricolaId == 1);
        var insumoOrigen = await db.Insumos.SingleAsync(x => x.InsumoId == 1);
        var insumoDestino = await db.Insumos.SingleAsync(x => x.InsumoId == 2);
        var costo = await db.Costos.SingleAsync(x => x.ReferenciaId == 1 && x.FuenteModulo == "Produccion.OperacionesAgricolas" && x.Estado == true);

        Assert.Equal(2, operacion.InsumoId);
        Assert.Equal(3m, operacion.CantidadInsumo);
        Assert.Equal(12m, operacion.CostoInsumo);
        Assert.Equal(7m, operacion.CostoManoObra);
        Assert.Equal(1m, operacion.CostoOtros);
        Assert.Equal(20m, operacion.CostoTotal);
        Assert.Equal("Aplicacion ajustada", operacion.Observaciones);
        Assert.Equal(10m, insumoOrigen.Cantidad);
        Assert.Equal(17m, insumoDestino.Cantidad);
        Assert.Equal(20m, costo.Monto);
    }

    [Fact]
    public async Task Create_Harvest_CreatesOperationWithoutReceptionOrLot()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var fecha = DateTime.UtcNow.Date;
        var response = await client.PostAsJsonAsync("/api/v1/produccion/operaciones-agricolas", new
        {
            cultivoId = 1,
            tipoOperacion = "Cosecha",
            fechaOperacion = fecha,
            cultivoConfiguracionCosechaId = 1,
            cantidad = 12m,
            costoManoObra = 0m,
            costoOtros = 5m,
            observaciones = "Nueva cosecha",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var operacion = await db.OperacionesAgricolas.OrderByDescending(x => x.OperacionAgricolaId).FirstAsync();
        var recepciones = await db.RecepcionesCafe.ToListAsync();
        var costos = await db.Costos
            .Where(x => x.FuenteModulo == "Produccion.OperacionesAgricolas" && x.ReferenciaId == operacion.OperacionAgricolaId)
            .ToListAsync();

        Assert.Equal("Cosecha", operacion.TipoOperacion);
        Assert.Equal("Cafe cereza", operacion.Cultivo);
        Assert.Null(operacion.ProductoId);
        Assert.Null(operacion.RecepcionCafeId);
        Assert.Null(operacion.LoteId);
        Assert.Equal(12m, operacion.Cantidad);
        Assert.Single(recepciones);
        Assert.Single(costos);
        Assert.Equal(5m, costos[0].Monto);
    }

    [Fact]
    public async Task Create_Harvest_WithoutConfiguredHarvestOption_ReturnsValidationProblem()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var fecha = DateTime.UtcNow.Date;
        var response = await client.PostAsJsonAsync("/api/v1/produccion/operaciones-agricolas", new
        {
            cultivoId = 1,
            tipoOperacion = "Cosecha",
            fechaOperacion = fecha,
            cantidad = 12m,
            costoManoObra = 0m,
            costoOtros = 5m,
            observaciones = "Nueva cosecha",
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CultivoConfiguracionCosechaId es requerido para una cosecha", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_Harvest_WithConfiguredHarvestOption_UsesSelectedHarvestType()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var fecha = DateTime.UtcNow.Date;
        var response = await client.PostAsJsonAsync("/api/v1/produccion/operaciones-agricolas", new
        {
            cultivoId = 1,
            tipoOperacion = "Cosecha",
            fechaOperacion = fecha,
            cultivoConfiguracionCosechaId = 2,
            cantidad = 7m,
            costoManoObra = 0m,
            costoOtros = 3m,
            observaciones = "Cosecha alternativa",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var operacion = await db.OperacionesAgricolas.OrderByDescending(x => x.OperacionAgricolaId).FirstAsync();

        Assert.Equal("Cafe lavado", operacion.Cultivo);
        Assert.Null(operacion.ProductoId);
        Assert.Null(operacion.RecepcionCafeId);
    }

    [Fact]
    public async Task Create_Harvest_WithForeignHarvestOption_ReturnsValidationProblem()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var fecha = DateTime.UtcNow.Date;
        var response = await client.PostAsJsonAsync("/api/v1/produccion/operaciones-agricolas", new
        {
            cultivoId = 1,
            tipoOperacion = "Cosecha",
            fechaOperacion = fecha,
            cultivoConfiguracionCosechaId = 999,
            cantidad = 7m,
            costoManoObra = 0m,
            costoOtros = 3m,
            observaciones = "Configuracion inválida",
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no pertenece al cultivo", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_HarvestWithAssociatedMovements_ReturnsClearValidationProblem()
    {
        await using var factory = new OperacionesAgricolasApiFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync("/api/v1/produccion/operaciones-agricolas/2/anular", content: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("recepción derivada ya tuvo procesos posteriores", body, StringComparison.Ordinal);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, "1");
        client.DefaultRequestHeaders.Add("X-CSRF", TestCsrfToken);
        client.DefaultRequestHeaders.Add("Cookie", $"pc_csrf={TestCsrfToken}");
        return client;
    }

    private sealed class OperacionesAgricolasApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"operaciones-agricolas-tests-{Guid.NewGuid():N}";

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
            var nowUtc = DateTime.UtcNow;

            db.Fincas.Add(new Finca
            {
                FincaId = 1,
                Nombre = "Finca Central",
                Estado = true,
            });

            db.Productos.Add(new Producto
            {
                ProductoId = 1,
                Nombre = "Cafe pergamino",
                Precio = 0m,
                PrecioInternet = 0m,
                PrecioMayorista = 0m,
                Stock = 25m,
                ManejaLotes = true,
                TipoLote = "Cafe",
                Estado = true,
                UnidadMedida = "kg",
                PermiteDecimal = true,
            });

            db.Productos.Add(new Producto
            {
                ProductoId = 2,
                Nombre = "Cafe cereza especial",
                Precio = 0m,
                PrecioInternet = 0m,
                PrecioMayorista = 0m,
                Stock = 0m,
                ManejaLotes = true,
                TipoLote = "Cafe",
                Estado = true,
                UnidadMedida = "kg",
                PermiteDecimal = true,
            });

            db.Cultivos.Add(new Cultivo
            {
                CultivoId = 1,
                FincaId = 1,
                ProductoCosechaId = 1,
                Nombre = "Lote Norte",
                Descripcion = "Cultivo principal",
                Estado = true,
            });

            db.CultivosConfiguracionesCosecha.Add(new CultivoConfiguracionCosecha
            {
                CultivoConfiguracionCosechaId = 1,
                CultivoId = 1,
                NombreTipoCosecha = "Cafe cereza",
                UnidadMedidaCodigo = "KG",
            });

            db.CultivosConfiguracionesCosecha.Add(new CultivoConfiguracionCosecha
            {
                CultivoConfiguracionCosechaId = 2,
                CultivoId = 1,
                NombreTipoCosecha = "Cafe lavado",
                UnidadMedidaCodigo = "KG",
            });

            db.Insumos.AddRange(
                new Insumo
                {
                    InsumoId = 1,
                    Nombre = "Fertilizante A",
                    Cantidad = 8m,
                    CostoUnitario = 5m,
                    UnidadMedida = "kg",
                    Estado = true,
                },
                new Insumo
                {
                    InsumoId = 2,
                    Nombre = "Fertilizante B",
                    Cantidad = 20m,
                    CostoUnitario = 4m,
                    UnidadMedida = "kg",
                    Estado = true,
                });

            db.Lotes.Add(new Lote
            {
                LoteId = 1,
                ProductoId = 1,
                CodigoLote = "COS-LOT-001",
                CantidadInicial = 15m,
                CantidadDisponible = 15m,
                Estado = "Activo",
                FechaCreacion = nowUtc.AddDays(-2),
            });

            db.OperacionesAgricolas.AddRange(
                new OperacionAgricola
                {
                    OperacionAgricolaId = 1,
                    CultivoId = 1,
                    TipoOperacion = "AplicacionInsumo",
                    FechaOperacion = nowUtc.AddDays(-3),
                    InsumoId = 1,
                    CantidadInsumo = 2m,
                    CostoInsumo = 10m,
                    CostoManoObra = 4m,
                    CostoOtros = 2m,
                    CostoTotal = 16m,
                    Observaciones = "Aplicacion inicial",
                    Estado = "Registrada",
                },
                new OperacionAgricola
                {
                    OperacionAgricolaId = 2,
                    CultivoId = 1,
                    TipoOperacion = "Cosecha",
                    FechaOperacion = nowUtc.AddDays(-2),
                    ProductoId = 1,
                    Cantidad = 15m,
                    Cultivo = "Cafe pergamino",
                    RecepcionCafeId = 1,
                    CostoInsumo = 0m,
                    CostoManoObra = 0m,
                    CostoOtros = 6m,
                    CostoTotal = 6m,
                    Observaciones = "Cosecha inicial",
                    Estado = "Registrada",
                });

            db.Costos.AddRange(
                new Costo
                {
                    CostoId = 1,
                    Nombre = "Costo operación AplicacionInsumo - cultivo Lote Norte",
                    TipoCosto = "OperacionAgricola",
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = 1,
                    CultivoId = 1,
                    Monto = 16m,
                    Fecha = nowUtc.AddDays(-3),
                    Estado = true,
                },
                new Costo
                {
                    CostoId = 2,
                    Nombre = "Costo cosecha COS-LOT-001",
                    TipoCosto = "CosechaCafe",
                    FuenteModulo = "Produccion.OperacionesAgricolas",
                    ReferenciaId = 2,
                    CultivoId = 1,
                    Monto = 6m,
                    Fecha = nowUtc.AddDays(-2),
                    Estado = true,
                });

            db.Proveedores.Add(new Proveedor
            {
                ProveedorId = 1,
                Nombre = "Finca Central / Lote Norte",
                Estado = true,
            });

            db.RecepcionesCafe.Add(new RecepcionCafe
            {
                RecepcionCafeId = 1,
                ProveedorId = 1,
                ProductoId = 1,
                TipoCafe = "Cereza",
                FechaRecepcion = nowUtc.AddDays(-2),
                CodigoLote = "COS-LOT-001",
                Cantidad = 15m,
                CantidadDisponible = 13m,
                PrecioUnitario = 0m,
                Total = 0m,
                Observaciones = "Recepción derivada",
                Estado = "Registrada",
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