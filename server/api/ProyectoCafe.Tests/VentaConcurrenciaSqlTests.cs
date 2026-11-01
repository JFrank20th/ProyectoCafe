using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Application.Services;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ProyectoCafe.Tests;

[Collection("SqlIntegration")]
public sealed class VentaConcurrenciaSqlTests : IClassFixture<ProyectoCafeSqlIntegrationFactory>
{
    private readonly ProyectoCafeSqlIntegrationFactory _factory;

    public VentaConcurrenciaSqlTests(ProyectoCafeSqlIntegrationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TwoCancellations_OnlyOneSucceeds()
    {
        var ventaId = await SeedSaleAsync($"conc-cancel-{Guid.NewGuid():N}@test.local");

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var service1 = scope1.ServiceProvider.GetRequiredService<VentaService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<VentaService>();

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task1 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await service1.CancelVentaAsync(ventaId, "Cancelación 1", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });
        var task2 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await service2.CancelVentaAsync(ventaId, "Cancelación 2", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        start.SetResult();
        var results = await Task.WhenAll(task1, task2);

        var successes = results.Count(r => r is OperationResult op && op.IsSuccess);
        Assert.Equal(1, successes);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var venta = await db.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        Assert.Equal(EstadoVenta.Cancelada, venta.Estado);

        var inverseCount = await db.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"CANCELACION-VENTA-{venta.NumeroVenta}");
        Assert.Equal(2, inverseCount);
    }

    [Fact]
    public async Task CancelAndAnulate_OnlyOneSucceeds()
    {
        var ventaId = await SeedSaleAsync($"conc-cancel-anulate-{Guid.NewGuid():N}@test.local");

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var cancelService = scope1.ServiceProvider.GetRequiredService<VentaService>();
        var anulateService = scope2.ServiceProvider.GetRequiredService<VentaService>();

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task1 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await cancelService.CancelVentaAsync(ventaId, "Cancelación", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });
        var task2 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await anulateService.AnulateVentaAsync(ventaId, "Anulación", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        start.SetResult();
        var results = await Task.WhenAll(task1, task2);

        var successes = results.Count(r => r is OperationResult op && op.IsSuccess);
        Assert.Equal(1, successes);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var venta = await db.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId);
        Assert.True(venta.Estado is EstadoVenta.Cancelada or EstadoVenta.Anulada);

        var cancelCount = await db.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"CANCELACION-VENTA-{venta.NumeroVenta}");
        var anulateCount = await db.MovimientosLoteCafe.AsNoTracking()
            .CountAsync(m => m.Referencia == $"ANULACION-VENTA-{venta.NumeroVenta}");

        if (venta.Estado == EstadoVenta.Cancelada)
        {
            Assert.Equal(2, cancelCount);
            Assert.Equal(0, anulateCount);
        }
        else
        {
            Assert.Equal(0, cancelCount);
            Assert.Equal(2, anulateCount);
        }
    }

    [Fact]
    public async Task PaymentAndCancel_AreConsistent()
    {
        var (ventaId, total) = await SeedSaleWithBalanceAsync($"conc-payment-cancel-{Guid.NewGuid():N}@test.local");

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var pagoService = scope1.ServiceProvider.GetRequiredService<PagoVentaService>();
        var ventaService = scope2.ServiceProvider.GetRequiredService<VentaService>();

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task1 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await pagoService.RegisterPagoAsync(
                    ventaId,
                    new CreatePagoVentaRequest
                    {
                        Monto = 10000m,
                        MetodoPago = "Efectivo",
                    },
                    null,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });
        var task2 = Task.Run<object>(async () =>
        {
            await start.Task;
            try
            {
                return await ventaService.CancelVentaAsync(ventaId, "Cancelación", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        start.SetResult();
        await Task.WhenAll(task1, task2);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var venta = await db.Ventas
            .AsNoTracking()
            .Include(v => v.Pagos)
            .SingleAsync(v => v.VentaId == ventaId);

        if (venta.Estado == EstadoVenta.Cancelada)
        {
            Assert.Empty(venta.Pagos);
            var inverseCount = await db.MovimientosLoteCafe.AsNoTracking()
                .CountAsync(m => m.Referencia == $"CANCELACION-VENTA-{venta.NumeroVenta}");
            Assert.Equal(2, inverseCount);
        }
        else
        {
            Assert.Single(venta.Pagos);
            Assert.Equal(10000m, venta.Pagos.Single().Monto);
        }
    }

    private async Task<int> SeedSaleAsync(string email)
    {
        using var client = await CreateAuthenticatedClientAsync(email);
        var catalogs = await SeedCatalogsAsync(client);
        var lote1Id = await CreateLotWithStockAsync(client, catalogs, $"CC1-{Guid.NewGuid():N}", 2);
        var lote2Id = await CreateLotWithStockAsync(client, catalogs, $"CC2-{Guid.NewGuid():N}", 2);
        var precioId = await CreatePriceAsync(client, catalogs);
        return await CreateSaleAsync(client, lote1Id, lote2Id, precioId, catalogs);
    }

    private async Task<(int VentaId, decimal Total)> SeedSaleWithBalanceAsync(string email)
    {
        var ventaId = await SeedSaleAsync(email);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        var total = (await db.Ventas.AsNoTracking().SingleAsync(v => v.VentaId == ventaId)).Total;
        return (ventaId, total);
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