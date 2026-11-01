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
                    loteCafeId = loteId,
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
            movimiento.Referencia == $"VENTA-{ventas[0].NumeroVenta}" &&
            movimiento.UsuarioId.HasValue);
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
                    loteCafeId = loteId,
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
                    loteCafeId = lote1Id,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 2,
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
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var existencias1 = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{lote1Id}/existencias");
        var existencias2 = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{lote2Id}/existencias");

        Assert.Equal(0, existencias1![0].CantidadPaquetes);
        Assert.Equal(1, existencias2![0].CantidadPaquetes);

        var movimientos1 = await client.GetFromJsonAsync<MovimientoResponse[]>(
            $"/api/v1/inventario/lotes/{lote1Id}/movimientos");
        var movimientos2 = await client.GetFromJsonAsync<MovimientoResponse[]>(
            $"/api/v1/inventario/lotes/{lote2Id}/movimientos");

        var salidas = movimientos1!.Concat(movimientos2!)
            .Where(movimiento => movimiento.TipoMovimiento == 2)
            .ToArray();
        Assert.Equal(2, salidas.Length);
        Assert.All(salidas, movimiento => Assert.True(movimiento.UsuarioId.HasValue));
        Assert.Single(salidas.Select(movimiento => movimiento.UsuarioId).Distinct());
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
                    loteCafeId = loteId,
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
                    loteCafeId = loteId,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 1,
                },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondSale.StatusCode);
    }

    [Fact]
    public async Task Venta_MissingLote_ReturnsBadRequest()
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
        await factory.SeedUserWithPermissionsAsync("ventas-missing-lote@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-missing-lote@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-MISSING",
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
                    cantidadPaquetes = 1,
                },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task Venta_PartialFailure_DoesNotDiscount()
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
        await factory.SeedUserWithPermissionsAsync("ventas-partial@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-partial@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var lote1Response = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-PARTIAL-1",
            fechaProduccion = DateTime.UtcNow.AddDays(-2),
        });
        var lote1Id = (await lote1Response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        var lote2Response = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-PARTIAL-2",
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
                    cantidadPaquetes = 5,
                },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);

        var existencias1 = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{lote1Id}/existencias");
        Assert.Equal(2, existencias1![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_Authorized_CancelsSale()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-endpoint@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-endpoint@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-CANCEL-ENDPOINT",
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
                    cantidadPaquetes = 1,
                },
            },
        });
        var ventaId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];

        var cancelResponse = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/cancelar", new
        {
            motivo = "Cancelación de prueba",
        });
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(2, venta!.Estado);
    }

    [Fact]
    public async Task Venta_AnulateEndpoint_Authorized_AnulatesSale()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Anular),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasPrecios, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
            (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-anulate-endpoint@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate-endpoint@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-ANULATE-ENDPOINT",
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
                    cantidadPaquetes = 1,
                },
            },
        });
        var ventaId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];

        var anulateResponse = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/anular", new
        {
            motivo = "Anulación de prueba",
        });
        Assert.Equal(HttpStatusCode.NoContent, anulateResponse.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(3, venta!.Estado);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_WithoutPermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-forbidden@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-forbidden@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/ventas/ventas/1/cancelar", new
        {
            motivo = "Motivo",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_EmptyMotivo_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-empty@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-empty@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/ventas/ventas/1/cancelar", new
        {
            motivo = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_NonExistent_ReturnsNotFound()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-nonexistent@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-nonexistent@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/ventas/ventas/999999/cancelar", new
        {
            motivo = "Motivo de prueba",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Venta_AnulateEndpoint_NonExistent_ReturnsNotFound()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Anular),
        };
        await factory.SeedUserWithPermissionsAsync("ventas-anulate-nonexistent@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate-nonexistent@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/ventas/ventas/999999/anular", new
        {
            motivo = "Motivo de prueba",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_MotivoLongerThan250_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
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
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-long@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-long@test.local");

        var (ventaId, loteId) = await CreateSaleForCancelAnulateAsync(factory, client);

        var response = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/cancelar", new
        {
            motivo = new string('A', 251),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(1, venta!.Estado);

        var existencias = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/existencias");
        Assert.Equal(3, existencias![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_AnulateEndpoint_MotivoLongerThan250_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Anular),
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
        await factory.SeedUserWithPermissionsAsync("ventas-anulate-long@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate-long@test.local");

        var (ventaId, loteId) = await CreateSaleForCancelAnulateAsync(factory, client);

        var response = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/anular", new
        {
            motivo = new string('B', 251),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(1, venta!.Estado);

        var existencias = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/existencias");
        Assert.Equal(3, existencias![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_CancelEndpoint_DeliveredSale_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Editar),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Cancelar),
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
        await factory.SeedUserWithPermissionsAsync("ventas-cancel-delivered@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel-delivered@test.local");

        var (ventaId, loteId) = await CreateSaleForCancelAnulateAsync(factory, client);

        var deliveryResponse = await client.PutAsJsonAsync(
            $"/api/v1/ventas/ventas/{ventaId}/estado-entrega",
            new { estadoEntrega = 4 });
        Assert.Equal(HttpStatusCode.NoContent, deliveryResponse.StatusCode);

        var response = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/cancelar", new
        {
            motivo = "Motivo de prueba",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(1, venta!.Estado);
        Assert.Equal(4, venta.EstadoEntrega);

        var existencias = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/existencias");
        Assert.Equal(3, existencias![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_AnulateEndpoint_DeliveredSale_ReturnsBadRequest()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var permissions = new (string Modulo, string Accion)[]
        {
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Ver),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Crear),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Editar),
            (Permissions.Modulos.VentasVentas, Permissions.Actions.Anular),
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
        await factory.SeedUserWithPermissionsAsync("ventas-anulate-delivered@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate-delivered@test.local");

        var (ventaId, loteId) = await CreateSaleForCancelAnulateAsync(factory, client);

        var deliveryResponse = await client.PutAsJsonAsync(
            $"/api/v1/ventas/ventas/{ventaId}/estado-entrega",
            new { estadoEntrega = 4 });
        Assert.Equal(HttpStatusCode.NoContent, deliveryResponse.StatusCode);

        var response = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/anular", new
        {
            motivo = "Motivo de prueba",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var venta = await client.GetFromJsonAsync<VentaResponse>($"/api/v1/ventas/ventas/{ventaId}");
        Assert.Equal(1, venta!.Estado);
        Assert.Equal(4, venta.EstadoEntrega);

        var existencias = await client.GetFromJsonAsync<ExistenciaResponse[]>(
            $"/api/v1/inventario/lotes/{loteId}/existencias");
        Assert.Equal(3, existencias![0].CantidadPaquetes);
    }

    [Fact]
    public async Task Venta_AnulateEndpoint_WithoutPermission_ReturnsForbidden()
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
        await factory.SeedUserWithPermissionsAsync("ventas-anulate-forbidden@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate-forbidden@test.local");

        var (ventaId, _) = await CreateSaleForCancelAnulateAsync(factory, client);

        var response = await client.PostAsJsonAsync($"/api/v1/ventas/ventas/{ventaId}/anular", new
        {
            motivo = "Motivo de prueba",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<(int VentaId, int LoteId)> CreateSaleForCancelAnulateAsync(
        ProyectoCafeApiFactory factory,
        HttpClient client)
    {
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = $"LC{Guid.NewGuid():N}",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        loteResponse.EnsureSuccessStatusCode();
        var loteId = (await loteResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["loteCafeId"];

        await client.PostAsJsonAsync($"/api/v1/inventario/lotes/{loteId}/movimientos", new
        {
            tipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
            presentacionCafeId = catalogs.Presentacion250Id,
            tipoCafeId = catalogs.TipoGranoId,
            cantidadPaquetes = 4,
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
                    loteCafeId = loteId,
                    presentacionCafeId = catalogs.Presentacion250Id,
                    tipoCafeId = catalogs.TipoGranoId,
                    precioVentaId = precioId,
                    cantidadPaquetes = 1,
                },
            },
        });
        var ventaId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];

        return (ventaId, loteId);
    }

    private sealed record VentaResponse(int VentaId, int NumeroVenta, decimal Total, int Estado, int EstadoEntrega, VentaDetalleResponse[] Detalles);
    private sealed record VentaDetalleResponse(int CantidadPaquetes, decimal PrecioUnitario, decimal Subtotal);
    private sealed record ExistenciaResponse(int CantidadPaquetes);
    private sealed record MovimientoResponse(int TipoMovimiento, int CantidadPaquetes, string? Referencia, int? UsuarioId);
}