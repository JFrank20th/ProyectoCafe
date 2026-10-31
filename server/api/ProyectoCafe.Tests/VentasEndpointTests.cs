using System.Net;
using System.Net.Http.Json;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class VentasEndpointTests
{
    [Fact]
    public async Task Venta_CreateAndList_ReturnsPersistedValues()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioExistencias, Permissions.Actions.Ver),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-admin@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-admin@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-VENTA",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        loteResponse.EnsureSuccessStatusCode();
        var loteBody = await loteResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var loteId = loteBody!["loteCafeId"];

        var movimientoResponse = await client.PostAsJsonAsync(
            $"/api/v1/inventario/lotes/{loteId}/movimientos",
            new
            {
                tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
                presentacionCafeId = catalogs.Presentacion250Id,
                tipoCafeId = catalogs.TipoGranoId,
                cantidadPaquetes = 5,
                referencia = (string?)null,
                observacion = (string?)null,
            });
        Assert.Equal(HttpStatusCode.Created, movimientoResponse.StatusCode);

        var precioResponse = await client.PostAsJsonAsync("/api/v1/ventas/precios", new
        {
            nombre = "Publico",
            presentacionCafeId = catalogs.Presentacion250Id,
            precio = 20000m,
            estado = EstadoCatalogoCafe.Activo,
        });
        Assert.Equal(HttpStatusCode.Created, precioResponse.StatusCode);
        var precioBody = await precioResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var precioId = precioBody!["precioVentaId"];

        var createResponse = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = "Venta de prueba",
            detalles = new[]
            {
                new
                {
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 2,
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var ventas = await client.GetFromJsonAsync<VentaResponse[]>("/api/v1/ventas/ventas");
        Assert.NotNull(ventas);
        Assert.Single(ventas!);
        Assert.Equal(2, ventas![0].Detalles[0].CantidadPaquetes);
        Assert.Equal(20000m, ventas[0].Detalles[0].PrecioUnitario);
        Assert.Equal(40000m, ventas[0].Total);

        var existencias = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/existencias");
        Assert.NotNull(existencias);
        Assert.Equal(3, existencias![0].CantidadPaquetes);

        var movimientos = await client.GetFromJsonAsync<MovimientoResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/movimientos");
        Assert.NotNull(movimientos);
        Assert.Contains(movimientos!, movimiento =>
            movimiento.TipoMovimiento == 2 &&
            movimiento.CantidadPaquetes == 2 &&
            movimiento.Referencia == $"VENTA-{ventas[0].NumeroVenta}");
    }

    [Fact]
    public async Task Venta_InsufficientStock_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-insufficient@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-insufficient@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-INSUF",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        loteResponse.EnsureSuccessStatusCode();
        var loteBody = await loteResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var loteId = loteBody!["loteCafeId"];

        await client.PostAsJsonAsync($"/api/v1/inventario/lotes/{loteId}/movimientos", new
        {
            tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
            presentacionCafeId = catalogs.Presentacion250Id,
            tipoCafeId = catalogs.TipoGranoId,
            cantidadPaquetes = 1,
            referencia = (string?)null,
            observacion = (string?)null,
        });

        var precioResponse = await client.PostAsJsonAsync("/api/v1/ventas/precios", new
        {
            nombre = "Publico",
            presentacionCafeId = catalogs.Presentacion250Id,
            precio = 20000m,
            estado = EstadoCatalogoCafe.Activo,
        });
        var precioId = (await precioResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["precioVentaId"];

        var createResponse = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = (string?)null,
            detalles = new[]
            {
                new
                {
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 2,
                },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task Venta_FifoDistribution_AcrossLots()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioExistencias, Permissions.Actions.Ver),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-fifo@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-fifo@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var lote1Response = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-FIFO-1",
            fechaProduccion = DateTime.UtcNow.AddDays(-2),
        });
        var lote1Id = (await lote1Response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        var lote2Response = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-FIFO-2",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        var lote2Id = (await lote2Response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        foreach (var loteId in new[] { lote1Id, lote2Id })
        {
            await client.PostAsJsonAsync($"/api/v1/inventario/lotes/{loteId}/movimientos", new
            {
                tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
                presentacionCafeId = catalogs.Presentacion250Id,
                tipoCafeId = catalogs.TipoGranoId,
                cantidadPaquetes = 2,
                referencia = (string?)null,
                observacion = (string?)null,
            });
        }

        var precioResponse = await client.PostAsJsonAsync("/api/v1/ventas/precios", new
        {
            nombre = "Publico",
            presentacionCafeId = catalogs.Presentacion250Id,
            precio = 20000m,
            estado = EstadoCatalogoCafe.Activo,
        });
        var precioId = (await precioResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["precioVentaId"];

        var createResponse = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = (string?)null,
            detalles = new[]
            {
                new
                {
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 3,
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var existencias1 = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{lote1Id}/existencias");
        var existencias2 = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{lote2Id}/existencias");

        Assert.Equal(0, existencias1![0].CantidadPaquetes);
        Assert.Equal(1, existencias2![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_SecondSaleAfterStockDepleted_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioExistencias, Permissions.Actions.Ver),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-second@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-second@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-SECOND",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        var loteId = (await loteResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        await client.PostAsJsonAsync($"/api/v1/inventario/lotes/{loteId}/movimientos", new
        {
            tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
            presentacionCafeId = catalogs.Presentacion250Id,
            tipoCafeId = catalogs.TipoGranoId,
            cantidadPaquetes = 2,
            referencia = (string?)null,
            observacion = (string?)null,
        });

        var precioResponse = await client.PostAsJsonAsync("/api/v1/ventas/precios", new
        {
            nombre = "Publico",
            presentacionCafeId = catalogs.Presentacion250Id,
            precio = 20000m,
            estado = EstadoCatalogoCafe.Activo,
        });
        var precioId = (await precioResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["precioVentaId"];

        var firstSale = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = (string?)null,
            detalles = new[]
            {
                new
                {
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 2,
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, firstSale.StatusCode);

        var secondSale = await client.PostAsJsonAsync("/api/v1/ventas/ventas", new
        {
            clienteId = (int?)null,
            fechaVenta = (DateTime?)null,
            observacion = (string?)null,
            detalles = new[]
            {
                new
                {
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 1,
                },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondSale.StatusCode);
    }

    private sealed record VentaResponse(int VentaId, int NumeroVenta, decimal Total, VentaDetalleResponse[] Detalles);
    private sealed record VentaDetalleResponse(int CantidadPaquetes, decimal PrecioUnitario, decimal Subtotal);
    private sealed record ExistenciaResponse(int CantidadPaquetes);
    private sealed record MovimientoResponse(int TipoMovimiento, int CantidadPaquetes, string? Referencia);
}