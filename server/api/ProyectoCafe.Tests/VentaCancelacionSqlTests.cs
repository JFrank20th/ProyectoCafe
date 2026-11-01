using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Security;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Services;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ProyectoCafe.Tests;

[Collection("SqlIntegration")]
public sealed class VentaCancelacionSqlTests : IClassFixture<ProyectoCafeSqlIntegrationFactory>
{
    private readonly ProyectoCafeSqlIntegrationFactory _factory;

    public VentaCancelacionSqlTests(ProyectoCafeSqlIntegrationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CancelVenta_AgotadoLote_BecomesActive()
    {
        var (ventaId, loteId) = await SeedSaleAsync($"sql-cancel-{Guid.NewGuid():N}@test.local", 2);

        using var scope = _factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var venta = await db.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        var stockAntes = await db.LotesCafeExistencias.AsNoTracking().Where(e => e.LoteCafeId == loteId).SumAsync(e => e.CantidadPaquetes);
        Assert.Equal(0, stockAntes);

        var result = await ventaService.CancelVentaAsync(ventaId, "Cancelaci�n SQL", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var stockDespues = await db.LotesCafeExistencias.AsNoTracking().Where(e => e.LoteCafeId == loteId).SumAsync(e => e.CantidadPaquetes);
        Assert.Equal(2, stockDespues);

        var loteDespues = await db.LotesCafeTerminado.AsNoTracking().SingleAsync(l => l.LoteCafeId == loteId);
        Assert.Equal(EstadoLoteCafe.Activo, loteDespues.Estado);

        var inversos = await db.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"CANCELACION-VENTA-{venta.NumeroVenta}");
        Assert.Equal(1, inversos);
    }

    [Fact]
    public async Task AnulateVenta_AgotadoLote_BecomesActive()
    {
        var (ventaId, loteId) = await SeedSaleAsync($"sql-anulate-{Guid.NewGuid():N}@test.local", 2);

        using var scope = _factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var venta = await db.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        var stockAntes = await db.LotesCafeExistencias.AsNoTracking().Where(e => e.LoteCafeId == loteId).SumAsync(e => e.CantidadPaquetes);
        Assert.Equal(0, stockAntes);

        var result = await ventaService.AnulateVentaAsync(ventaId, "Anulaci�n SQL", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var stockDespues = await db.LotesCafeExistencias.AsNoTracking().Where(e => e.LoteCafeId == loteId).SumAsync(e => e.CantidadPaquetes);
        Assert.Equal(2, stockDespues);

        var loteDespues = await db.LotesCafeTerminado.AsNoTracking().SingleAsync(l => l.LoteCafeId == loteId);
        Assert.Equal(EstadoLoteCafe.Activo, loteDespues.Estado);

        var inversos = await db.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"ANULACION-VENTA-{venta.NumeroVenta}");
        Assert.Equal(1, inversos);
    }

    private async Task<(int VentaId, int LoteId)> SeedSaleAsync(string email, int cantidad)
    {
        using var client = await CreateAuthenticatedClientAsync(email);
        var catalogs = await SeedCatalogsAsync(client);
        var loteId = await CreateLotWithStockAsync(client, catalogs, $"SQL-{Guid.NewGuid():N}", cantidad);
        var precioId = await CreatePriceAsync(client, catalogs);
        var ventaId = await CreateSaleAsync(client, loteId, precioId, catalogs, cantidad);
        return (ventaId, loteId);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await SeedUserAsync(email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = ProyectoCafeApiFactory.DefaultPassword,
        });
        response.EnsureSuccessStatusCode();

        var csrfToken = ReadCookie(response, "pc_csrf");
        if (!string.IsNullOrWhiteSpace(csrfToken))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-CSRF", csrfToken);
        }

        return client;
    }

    private async Task SeedUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new Usuario
        {
            Email = normalizedEmail,
            EmailNormalizado = normalizedEmail,
            Nombre = $"Test {normalizedEmail}",
            PasswordHash = PasswordHasher.HashPassword(ProyectoCafeApiFactory.DefaultPassword, iterations: 10_000),
            Estado = UsuarioEstado.Activo,
            CreadoEn = DateTime.UtcNow,
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var role = new Rol { Nombre = $"Role-{Guid.NewGuid():N}", Estado = RolEstado.Activo };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = user.UsuarioId, RolId = role.RolId });

        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Anular),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioExistencias, Permissions.Actions.Ver),
        };

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
    }

    private static async Task<(int Presentacion250Id, int TipoGranoId)> SeedCatalogsAsync(HttpClient client)
    {
        var presentacionResponse = await client.PostAsJsonAsync("/api/v1/inventario/presentaciones", new
        {
            codigo = $"P{Guid.NewGuid():N}".Substring(0, 8),
            nombre = $"Presentaci�n {Guid.NewGuid():N}",
            pesoUnitarioGramos = 250,
            estado = EstadoCatalogoCafe.Activo,
        });
        presentacionResponse.EnsureSuccessStatusCode();
        var presentacionId = (await presentacionResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["presentacionCafeId"];

        var tipoResponse = await client.PostAsJsonAsync("/api/v1/inventario/tipos-cafe", new
        {
            codigo = $"G{Guid.NewGuid():N}".Substring(0, 8),
            nombre = $"Grano {Guid.NewGuid():N}",
            estado = EstadoCatalogoCafe.Activo,
        });
        tipoResponse.EnsureSuccessStatusCode();
        var tipoId = (await tipoResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["tipoCafeId"];

        return (presentacionId, tipoId);
    }

    private async Task<int> CreateLotWithStockAsync(
        HttpClient client,
        (int Presentacion250Id, int TipoGranoId) catalogs,
        string codigo,
        int cantidad)
    {
        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = codigo,
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        loteResponse.EnsureSuccessStatusCode();
        var loteId = (await loteResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        var movimientoResponse = await client.PostAsJsonAsync($"/api/v1/inventario/lotes/{loteId}/movimientos", new
        {
            tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
            presentacionCafeId = catalogs.Presentacion250Id,
            tipoCafeId = catalogs.TipoGranoId,
            cantidadPaquetes = cantidad,
            referencia = (string?)null,
            observacion = (string?)null,
        });
        movimientoResponse.EnsureSuccessStatusCode();

        return loteId;
    }

    private async Task<int> CreatePriceAsync(
        HttpClient client,
        (int Presentacion250Id, int TipoGranoId) catalogs)
    {
        var precioResponse = await client.PostAsJsonAsync("/api/v1/ventas/precios", new
        {
            nombre = "Publico",
            presentacionCafeId = catalogs.Presentacion250Id,
            precio = 20000m,
            estado = EstadoCatalogoCafe.Activo,
        });
        precioResponse.EnsureSuccessStatusCode();
        return (await precioResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["precioVentaId"];
    }

    private async Task<int> CreateSaleAsync(
        HttpClient client,
        int loteId,
        int precioId,
        (int Presentacion250Id, int TipoGranoId) catalogs,
        int cantidad)
    {
        var createResponse = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = (string?)null,
            detalles = new[]
            {
                new
                {
                    loteCafeId = loteId,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = cantidad,
                },
            },
        });
        createResponse.EnsureSuccessStatusCode();
        return (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];
    }

    private static string ReadCookie(HttpResponseMessage response, string cookieName)
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
}
