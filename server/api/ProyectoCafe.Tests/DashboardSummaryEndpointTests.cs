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
    public async Task GetSummary_AppliesRequestedPeriodToMetrics()
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
        Assert.Equal(1, summary.Production.ActiveLotsCount);
        Assert.Equal(1, summary.Production.LotsWithStockCount);
        Assert.Equal(120m, summary.Production.AvailableQuantity);
        Assert.Equal("kg", summary.Production.AvailableUnit);
        Assert.Equal(5, summary.Production.ProcessRecordsCount);

        Assert.Equal(1, summary.Commercial.ActiveSalesCount);
        Assert.Equal(500m, summary.Commercial.ActiveSalesTotal);
        Assert.Equal(1, summary.Commercial.ActiveCustomersCount);

        Assert.Equal(200m, summary.Finance.GrossMarginAmount);
        Assert.Equal(40m, summary.Finance.GrossMarginPercent);
        Assert.Equal(50m, summary.Finance.ActiveCostsTotal);

        Assert.Equal(1, summary.Traceability.ActiveLotsCount);
        Assert.Equal(1, summary.Traceability.TraceableLotsCount);
        Assert.Equal(100m, summary.Traceability.CoveragePercent);

        Assert.NotNull(summary.PreviousPeriod);
        Assert.Equal("30d", summary.PreviousPeriod!.Window.Period);
        Assert.Equal(1, summary.PreviousPeriod.Production.ActiveLotsCount);
        Assert.Equal(80m, summary.PreviousPeriod.Production.AvailableQuantity);
        Assert.Equal(900m, summary.PreviousPeriod.Commercial.ActiveSalesTotal);
        Assert.Equal(500m, summary.PreviousPeriod.Finance.GrossMarginAmount);
        Assert.Equal(55.6m, summary.PreviousPeriod.Finance.GrossMarginPercent);
        Assert.Equal(100m, summary.PreviousPeriod.Traceability.CoveragePercent);
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
        Assert.NotNull(summary.PreviousPeriod);
        Assert.Equal("custom", summary.PreviousPeriod!.Window.Period);
        Assert.Equal(1, summary.Production.ActiveLotsCount);
        Assert.Equal(3, summary.Production.ProcessRecordsCount);
        Assert.Equal(900m, summary.Commercial.ActiveSalesTotal);
        Assert.Equal(55.6m, summary.Finance.GrossMarginPercent);
        Assert.Equal(0m, summary.PreviousPeriod.Commercial.ActiveSalesTotal);
        Assert.Equal(1, summary.PreviousPeriod.Production.ProcessRecordsCount);
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

            db.Productos.Add(new Producto
            {
                ProductoId = 1,
                Nombre = "Cafe pergamino",
                Precio = 0m,
                PrecioInternet = 0m,
                PrecioMayorista = 0m,
                Stock = 200m,
                ManejaLotes = true,
                TipoLote = "Cafe",
                Estado = true,
                UnidadMedida = "kg",
                PermiteDecimal = true,
            });

            db.Lotes.AddRange(
                new Lote
                {
                    LoteId = 1,
                    ProductoId = 1,
                    CodigoLote = "LOT-30D",
                    CantidadInicial = 140m,
                    CantidadDisponible = 120m,
                    Estado = "Activo",
                    FechaCreacion = nowUtc.AddDays(-5),
                },
                new Lote
                {
                    LoteId = 2,
                    ProductoId = 1,
                    CodigoLote = "LOT-90D",
                    CantidadInicial = 100m,
                    CantidadDisponible = 80m,
                    Estado = "Activo",
                    FechaCreacion = nowUtc.AddDays(-45),
                },
                new Lote
                {
                    LoteId = 3,
                    ProductoId = 1,
                    CodigoLote = "LOT-INACTIVO",
                    CantidadInicial = 50m,
                    CantidadDisponible = 50m,
                    Estado = "Inactivo",
                    FechaCreacion = nowUtc.AddDays(-2),
                });

            db.CosechasCafe.AddRange(
                new CosechaCafe
                {
                    CosechaCafeId = 1,
                    CultivoId = 1,
                    ProductoId = 1,
                    Cultivo = "Arabica",
                    FechaCosecha = nowUtc.AddDays(-4),
                    Cantidad = 60m,
                    Estado = "Registrada",
                    LoteId = 1,
                },
                new CosechaCafe
                {
                    CosechaCafeId = 2,
                    CultivoId = 1,
                    ProductoId = 1,
                    Cultivo = "Arabica",
                    FechaCosecha = nowUtc.AddDays(-40),
                    Cantidad = 55m,
                    Estado = "Registrada",
                    LoteId = 2,
                });

            db.RecepcionesCafe.AddRange(
                new RecepcionCafe
                {
                    RecepcionCafeId = 1,
                    ProveedorId = 1,
                    ProductoId = 1,
                    TipoCafe = "Oreado",
                    FechaRecepcion = nowUtc.AddDays(-3),
                    CodigoLote = "REC-001",
                    Cantidad = 40m,
                    CantidadDisponible = 40m,
                    PrecioUnitario = 2m,
                    Total = 80m,
                    Estado = "Registrada",
                    LoteId = 1,
                },
                new RecepcionCafe
                {
                    RecepcionCafeId = 2,
                    ProveedorId = 1,
                    ProductoId = 1,
                    TipoCafe = "Oreado",
                    FechaRecepcion = nowUtc.AddDays(-50),
                    CodigoLote = "REC-002",
                    Cantidad = 35m,
                    CantidadDisponible = 35m,
                    PrecioUnitario = 2m,
                    Total = 70m,
                    Estado = "Registrada",
                    LoteId = 2,
                });

            db.ProcesosInventario.AddRange(
                new ProcesoInventario
                {
                    ProcesoInventarioId = 1,
                    RecepcionCafeId = 1,
                    LoteOrigenId = 1,
                    ProductoSalidaId = 1,
                    LoteSalidaId = 1,
                    FechaProceso = nowUtc.AddDays(-2),
                    UsaLoteCompleto = false,
                    CantidadOrigenConsumida = 50m,
                    CantidadSalida = 45m,
                    CostoProceso = 10m,
                    Estado = "Registrado",
                },
                new ProcesoInventario
                {
                    ProcesoInventarioId = 2,
                    RecepcionCafeId = 2,
                    LoteOrigenId = 2,
                    ProductoSalidaId = 1,
                    LoteSalidaId = 2,
                    FechaProceso = nowUtc.AddDays(-45),
                    UsaLoteCompleto = false,
                    CantidadOrigenConsumida = 40m,
                    CantidadSalida = 36m,
                    CostoProceso = 8m,
                    Estado = "Registrado",
                },
                new ProcesoInventario
                {
                    ProcesoInventarioId = 3,
                    RecepcionCafeId = 1,
                    LoteOrigenId = 1,
                    ProductoSalidaId = 1,
                    LoteSalidaId = 1,
                    FechaProceso = nowUtc.AddDays(-1),
                    UsaLoteCompleto = false,
                    CantidadOrigenConsumida = 20m,
                    CantidadSalida = 80m,
                    CostoProceso = 12m,
                    Estado = "Registrado",
                },
                new ProcesoInventario
                {
                    ProcesoInventarioId = 4,
                    RecepcionCafeId = 2,
                    LoteOrigenId = 2,
                    ProductoSalidaId = 1,
                    LoteSalidaId = 2,
                    FechaProceso = nowUtc.AddDays(-42),
                    UsaLoteCompleto = false,
                    CantidadOrigenConsumida = 18m,
                    CantidadSalida = 70m,
                    CostoProceso = 10m,
                    Estado = "Registrado",
                });

            db.OperacionesAgricolas.Add(new OperacionAgricola
            {
                OperacionAgricolaId = 1,
                CultivoId = 1,
                TipoOperacion = "Cosecha",
                FechaOperacion = nowUtc.AddDays(-6),
                Cantidad = 22m,
                Cultivo = "Cafe cereza",
                CostoInsumo = 0m,
                CostoManoObra = 0m,
                CostoOtros = 4m,
                CostoTotal = 4m,
                Observaciones = "Cosecha sin lote inmediato",
                Estado = "Registrada",
            });

            db.LoteMovimientos.AddRange(
                new LoteMovimiento
                {
                    MovimientoId = 1,
                    LoteId = 1,
                    Tipo = "Salida",
                    Fecha = nowUtc.AddDays(-1),
                    Cantidad = 10m,
                },
                new LoteMovimiento
                {
                    MovimientoId = 2,
                    LoteId = 2,
                    Tipo = "Salida",
                    Fecha = nowUtc.AddDays(-41),
                    Cantidad = 8m,
                });

            db.Ventas.AddRange(
                new Venta
                {
                    VentaId = 1,
                    ClienteId = 10,
                    Fecha = nowUtc.AddDays(-4),
                    Total = 500m,
                    EstadoPago = "Pagado",
                    TipoPrecio = "Normal",
                    Estado = true,
                },
                new Venta
                {
                    VentaId = 2,
                    ClienteId = 20,
                    Fecha = nowUtc.AddDays(-45),
                    Total = 900m,
                    EstadoPago = "Pagado",
                    TipoPrecio = "Normal",
                    Estado = true,
                },
                new Venta
                {
                    VentaId = 3,
                    ClienteId = 30,
                    Fecha = nowUtc.AddDays(-2),
                    Total = 700m,
                    EstadoPago = "Pagado",
                    TipoPrecio = "Normal",
                    Estado = false,
                });

            db.VentaDetalles.AddRange(
                new VentaDetalle
                {
                    DetalleId = 1,
                    VentaId = 1,
                    ProductoId = 1,
                    LoteId = 1,
                    Cantidad = 10m,
                    PrecioUnitario = 50m,
                    CostoUnitario = 30m,
                    Subtotal = 500m,
                },
                new VentaDetalle
                {
                    DetalleId = 2,
                    VentaId = 2,
                    ProductoId = 1,
                    LoteId = 2,
                    Cantidad = 20m,
                    PrecioUnitario = 45m,
                    CostoUnitario = 20m,
                    Subtotal = 900m,
                },
                new VentaDetalle
                {
                    DetalleId = 3,
                    VentaId = 3,
                    ProductoId = 1,
                    LoteId = 1,
                    Cantidad = 14m,
                    PrecioUnitario = 50m,
                    CostoUnitario = 10m,
                    Subtotal = 700m,
                });

            db.Costos.AddRange(
                new Costo
                {
                    CostoId = 1,
                    Nombre = "Secado reciente",
                    TipoCosto = "Proceso",
                    Monto = 50m,
                    Fecha = nowUtc.AddDays(-4),
                    Estado = true,
                },
                new Costo
                {
                    CostoId = 2,
                    Nombre = "Secado anterior",
                    TipoCosto = "Proceso",
                    Monto = 80m,
                    Fecha = nowUtc.AddDays(-40),
                    Estado = true,
                },
                new Costo
                {
                    CostoId = 3,
                    Nombre = "Costo inactivo",
                    TipoCosto = "Proceso",
                    Monto = 999m,
                    Fecha = nowUtc.AddDays(-3),
                    Estado = false,
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
}
