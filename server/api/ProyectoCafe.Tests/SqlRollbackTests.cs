using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.Application.Services;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ProyectoCafe.Tests;

[Collection("SqlIntegration")]
public sealed class SqlRollbackTests : IClassFixture<ProyectoCafeSqlFaultInjectionFactory>
{
    private readonly ProyectoCafeSqlFaultInjectionFactory _factory;

    public SqlRollbackTests(ProyectoCafeSqlFaultInjectionFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CancelVenta_RollbacksWhenMovementInsertFails()
    {
        await RunRollbackScenarioAsync(isAnnulation: false);
    }

    [Fact]
    public async Task AnulateVenta_RollbacksWhenMovementInsertFails()
    {
        await RunRollbackScenarioAsync(isAnnulation: true);
    }

    private async Task RunRollbackScenarioAsync(bool isAnnulation)
    {
        using var client = await CreateAuthenticatedClientAsync();

        var catalogs = await SeedCatalogsAsync(client);

        var lote1Id = await CreateLotWithStockAsync(client, catalogs, $"RB1-{Guid.NewGuid():N}", 2);
        var lote2Id = await CreateLotWithStockAsync(client, catalogs, $"RB2-{Guid.NewGuid():N}", 2);

        var precioId = await CreatePriceAsync(client, catalogs);

        var ventaId = await CreateSaleAsync(client, lote1Id, lote2Id, precioId, catalogs);

        using var initialScope = _factory.Services.CreateScope();
        var initialDb = initialScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var initialVenta = await initialDb.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        var initialStock1 = await initialDb.LotesCafeExistencias.AsNoTracking()
            .SingleAsync(e => e.LoteCafeId == lote1Id);
        var initialStock2 = await initialDb.LotesCafeExistencias.AsNoTracking()
            .SingleAsync(e => e.LoteCafeId == lote2Id);

        using var serviceScope = _factory.Services.CreateScope();
        var ventaService = serviceScope.ServiceProvider.GetRequiredService<VentaService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            isAnnulation
                ? ventaService.AnulateVentaAsync(ventaId, "Motivo de prueba", null, CancellationToken.None)
                : ventaService.CancelVentaAsync(ventaId, "Motivo de prueba", null, CancellationToken.None));

        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var finalVenta = await finalDb.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        Assert.Equal(EstadoVenta.Registrada, finalVenta.Estado);

        var finalStock1 = await finalDb.LotesCafeExistencias.AsNoTracking()
            .SingleAsync(e => e.LoteCafeId == lote1Id);
        var finalStock2 = await finalDb.LotesCafeExistencias.AsNoTracking()
            .SingleAsync(e => e.LoteCafeId == lote2Id);

        Assert.Equal(initialStock1.CantidadPaquetes, finalStock1.CantidadPaquetes);
        Assert.Equal(initialStock2.CantidadPaquetes, finalStock2.CantidadPaquetes);

        var prefix = isAnnulation ? "ANULACION" : "CANCELACION";
        var inverseCount = await finalDb.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"{prefix}-VENTA-{initialVenta.NumeroVenta}");
        Assert.Equal(0, inverseCount);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var email = $"rollback-{Guid.NewGuid():N}@test.local";
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
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Editar),
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
        // Los catálogos se crean mediante endpoints existentes.
        var presentacionResponse = await client.PostAsJsonAsync("/api/v1/inventario/presentaciones", new
        {
            codigo = $"P{Guid.NewGuid():N}".Substring(0, 8),
            nombre = $"Presentación {Guid.NewGuid():N}",
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
        int lote1Id,
        int lote2Id,
        int precioId,
        (int Presentacion250Id, int TipoGranoId) catalogs)
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
                    loteCafeId = lote1Id,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 1,
                },
                new
                {
                    loteCafeId = lote2Id,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 1,
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