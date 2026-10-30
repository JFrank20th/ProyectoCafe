using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class InventarioCafeEndpointTests
{
    private static readonly (string Modulo, string Accion)[] AllInventoryPermissions =
    [
        (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver),
        (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear),
        (Permissions.Modulos.InventarioExistencias, Permissions.Actions.Ver),
        (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Ver),
        (Permissions.Modulos.InventarioMovimientos, Permissions.Actions.Crear),
        (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver),
        (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Crear),
        (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Editar),
        (Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Eliminar),
        (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Ver),
        (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Crear),
        (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Editar),
        (Permissions.Modulos.InventarioTiposCafe, Permissions.Actions.Eliminar),
    ];

    [Fact]
    public async Task Lotes_CreateListAndDetail_ReturnPersistedValues()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");

        var loteId = await CreateLoteAsync(client, "lote-001");

        var lotes = await GetLotesAsync(client);
        var lote = Assert.Single(lotes);
        Assert.Equal(loteId, lote.LoteCafeId);
        Assert.Equal("LOTE-001", lote.CodigoLote);
        Assert.Equal(EstadoLoteCafe.Activo, lote.Estado);
        Assert.Equal(0, lote.TotalPaquetes);

        var detail = await GetLoteAsync(client, loteId);
        Assert.Equal("LOTE-001", detail.CodigoLote);

        var duplicate = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-001",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });

        await AssertValidationProblemAsync(duplicate, nameof(CreateLoteCafeRequest.CodigoLote));
    }

    [Fact]
    public async Task Ingresos_SeAcumulanParaLaMismaCombinacion()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();
        var loteId = await CreateLoteAsync(client, "LOTE-ACUMULADO");

        var first = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 10);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 5);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var existencias = await GetExistenciasAsync(client, loteId);
        var existencia = Assert.Single(existencias);
        Assert.Equal(15, existencia.CantidadPaquetes);
        Assert.Equal(250, existencia.PesoUnitarioGramos);
        Assert.Equal(3.750m, existencia.PesoNetoKg);

        var movimientos = await GetMovimientosAsync(client, loteId);
        Assert.Equal(2, movimientos.Length);
        Assert.All(movimientos, movimiento =>
            Assert.Equal(TipoMovimientoLoteCafe.Ingreso, movimiento.TipoMovimiento));

        var detail = await GetLoteAsync(client, loteId);
        Assert.Equal(15, detail.TotalPaquetes);
        Assert.Equal(3.750m, detail.TotalPesoNetoKg);
        Assert.Equal(EstadoLoteCafe.Activo, detail.Estado);
    }

    [Fact]
    public async Task MultiplesCombinaciones_SeRegistranYTotalizanIndependientemente()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();
        var loteId = await CreateLoteAsync(client, "LOTE-COMBINADO");

        Assert.Equal(HttpStatusCode.Created, (await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 10)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion454Id, catalogs.TipoMolidoId, 4)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoMolidoId, 2)).StatusCode);

        var existencias = await GetExistenciasAsync(client, loteId);
        Assert.Equal(3, existencias.Length);

        var grano250 = existencias.Single(x =>
            x.PresentacionCodigo == "250G" && x.TipoCafeCodigo == "GRANO");
        var molido454 = existencias.Single(x =>
            x.PresentacionCodigo == "454G" && x.TipoCafeCodigo == "MOLIDO");
        var molido250 = existencias.Single(x =>
            x.PresentacionCodigo == "250G" && x.TipoCafeCodigo == "MOLIDO");

        Assert.Equal(10, grano250.CantidadPaquetes);
        Assert.Equal(2.500m, grano250.PesoNetoKg);
        Assert.Equal(4, molido454.CantidadPaquetes);
        Assert.Equal(1.816m, molido454.PesoNetoKg);
        Assert.Equal(2, molido250.CantidadPaquetes);

        var detail = await GetLoteAsync(client, loteId);
        Assert.Equal(16, detail.TotalPaquetes);
        Assert.Equal(4.816m, detail.TotalPesoNetoKg);

        var resumen = Assert.Single(await GetLotesAsync(client));
        Assert.Equal(16, resumen.TotalPaquetes);
        Assert.Equal(4.816m, resumen.TotalPesoNetoKg);
    }

    [Fact]
    public async Task Salidas_ReducenExistenciasYActualizanEstadoDelLote()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();
        var loteId = await CreateLoteAsync(client, "LOTE-SALIDAS");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 10);

        var salida = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 4);
        Assert.Equal(HttpStatusCode.Created, salida.StatusCode);

        var existencia = Assert.Single(await GetExistenciasAsync(client, loteId));
        Assert.Equal(6, existencia.CantidadPaquetes);

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 6);

        var detail = await GetLoteAsync(client, loteId);
        Assert.Equal(0, detail.TotalPaquetes);
        Assert.Equal(EstadoLoteCafe.Agotado, detail.Estado);

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 2);

        detail = await GetLoteAsync(client, loteId);
        Assert.Equal(2, detail.TotalPaquetes);
        Assert.Equal(EstadoLoteCafe.Activo, detail.Estado);
    }

    [Fact]
    public async Task MovimientoInvalido_NoRegistraMovimientoNiModificaExistencias()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();
        var loteId = await CreateLoteAsync(client, "LOTE-TRANSACCION");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 3);

        var salidaInsuficiente = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 4);
        await AssertValidationProblemAsync(
            salidaInsuficiente, nameof(RegistrarMovimientoRequest.CantidadPaquetes));

        var cantidadCero = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 0);
        await AssertValidationProblemAsync(
            cantidadCero, nameof(RegistrarMovimientoRequest.CantidadPaquetes));

        var presentacionInexistente = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            999999, catalogs.TipoGranoId, 1);
        await AssertValidationProblemAsync(
            presentacionInexistente, nameof(RegistrarMovimientoRequest.PresentacionCafeId));

        var tipoInexistente = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, 999999, 1);
        await AssertValidationProblemAsync(
            tipoInexistente, nameof(RegistrarMovimientoRequest.TipoCafeId));

        var loteInexistente = await RegisterMovementAsync(
            client, 999999, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 1);
        Assert.Equal(HttpStatusCode.NotFound, loteInexistente.StatusCode);

        var existencia = Assert.Single(await GetExistenciasAsync(client, loteId));
        Assert.Equal(3, existencia.CantidadPaquetes);
        Assert.Single(await GetMovimientosAsync(client, loteId));

        await factory.SetLoteCafeEstadoAsync(loteId, EstadoLoteCafe.Bloqueado);
        var loteBloqueado = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 1);
        await AssertValidationProblemAsync(loteBloqueado, "Estado");

        existencia = Assert.Single(await GetExistenciasAsync(client, loteId));
        Assert.Equal(3, existencia.CantidadPaquetes);
        Assert.Single(await GetMovimientosAsync(client, loteId));
    }

    [Fact]
    public async Task Ajustes_RequierenJustificacionYRespetanElSaldo()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();
        var loteId = await CreateLoteAsync(client, "LOTE-AJUSTES");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 10);

        var positivo = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.AjustePositivo,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 2, "Corrección de conteo");
        Assert.Equal(HttpStatusCode.Created, positivo.StatusCode);

        var negativo = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.AjusteNegativo,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 5, "Merma detectada");
        Assert.Equal(HttpStatusCode.Created, negativo.StatusCode);

        var sinJustificacion = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.AjustePositivo,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 1);
        await AssertValidationProblemAsync(
            sinJustificacion, nameof(RegistrarMovimientoRequest.Observacion));

        var negativoInsuficiente = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.AjusteNegativo,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 100, "Merma");
        await AssertValidationProblemAsync(
            negativoInsuficiente, nameof(RegistrarMovimientoRequest.CantidadPaquetes));

        var existencia = Assert.Single(await GetExistenciasAsync(client, loteId));
        Assert.Equal(7, existencia.CantidadPaquetes);

        var movimientos = await GetMovimientosAsync(client, loteId);
        Assert.Equal(3, movimientos.Length);
        Assert.Contains(movimientos, x => x.TipoMovimiento == TipoMovimientoLoteCafe.AjustePositivo);
        Assert.Contains(movimientos, x => x.TipoMovimiento == TipoMovimientoLoteCafe.AjusteNegativo);
    }

    [Fact]
    public async Task CatalogosInactivos_NoAdmitenMovimientos()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var presentacionActiva = await factory.SeedPresentacionCafeAsync("250G", "250 g", 250);
        var presentacionInactiva = await factory.SeedPresentacionCafeAsync(
            "454G", "454 g", 454, EstadoCatalogoCafe.Inactivo);
        var tipoActivo = await factory.SeedTipoCafeAsync("GRANO", "Grano");
        var tipoInactivo = await factory.SeedTipoCafeAsync(
            "MOLIDO", "Molido", EstadoCatalogoCafe.Inactivo);
        var loteId = await CreateLoteAsync(client, "LOTE-CATALOGOS");

        var presentacionInvalida = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            presentacionInactiva, tipoActivo, 1);
        await AssertValidationProblemAsync(
            presentacionInvalida, nameof(RegistrarMovimientoRequest.PresentacionCafeId));

        var tipoInvalido = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            presentacionActiva, tipoInactivo, 1);
        await AssertValidationProblemAsync(
            tipoInvalido, nameof(RegistrarMovimientoRequest.TipoCafeId));

        Assert.Empty(await GetMovimientosAsync(client, loteId));
        Assert.Empty(await GetExistenciasAsync(client, loteId));
    }

    [Fact]
    public async Task FechasYReferencias_SeValidan()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");
        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var produccionFutura = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-FUTURO",
            fechaProduccion = DateTime.UtcNow.AddDays(1),
        });
        await AssertValidationProblemAsync(
            produccionFutura, nameof(CreateLoteCafeRequest.FechaProduccion));

        var fechaProduccion = DateTime.UtcNow.AddDays(-10);
        var envasadoInvalido = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-ENVASADO",
            fechaProduccion,
            fechaEnvasado = fechaProduccion.AddDays(-1),
        });
        await AssertValidationProblemAsync(
            envasadoInvalido, nameof(CreateLoteCafeRequest.FechaEnvasado));

        var vencimientoInvalido = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-VENCIMIENTO",
            fechaProduccion,
            fechaEnvasado = fechaProduccion.AddDays(1),
            fechaVencimiento = fechaProduccion,
        });
        await AssertValidationProblemAsync(
            vencimientoInvalido, nameof(CreateLoteCafeRequest.FechaVencimiento));

        var loteId = await CreateLoteAsync(client, "LOTE-FECHAS");
        var movimientoFuturo = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 1,
            fechaMovimiento: DateTime.UtcNow.AddDays(1));
        await AssertValidationProblemAsync(
            movimientoFuturo, nameof(RegistrarMovimientoRequest.FechaMovimiento));

        var movimientoAntesDeProduccion = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            catalogs.Presentacion250Id, catalogs.TipoGranoId, 1,
            fechaMovimiento: DateTime.UtcNow.AddDays(-20));
        await AssertValidationProblemAsync(
            movimientoAntesDeProduccion, nameof(RegistrarMovimientoRequest.FechaMovimiento));
    }

    [Fact]
    public async Task Presentacion_ConservaHistorialYNoSeEliminaConStock()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");

        var presentacionId = await CreatePresentacionAsync(client, "250G", "250 g", 250);
        var tipoId = await CreateTipoCafeAsync(client, "GRANO", "Grano");
        var loteId = await CreateLoteAsync(client, "LOTE-PRESENTACION");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            presentacionId, tipoId, 10);

        var pesoConHistorial = await client.PutAsJsonAsync(
            $"/api/v1/inventario/presentaciones/{presentacionId}",
            new { codigo = "250G", nombre = "250 g", pesoUnitarioGramos = 300 });
        await AssertValidationProblemAsync(
            pesoConHistorial, nameof(UpsertPresentacionCafeRequest.PesoUnitarioGramos));

        var inactivarConStock = await client.PutAsJsonAsync(
            $"/api/v1/inventario/presentaciones/{presentacionId}",
            new
            {
                codigo = "250G",
                nombre = "250 g",
                pesoUnitarioGramos = 250,
                estado = (int)EstadoCatalogoCafe.Inactivo,
            });
        await AssertValidationProblemAsync(
            inactivarConStock, nameof(UpsertPresentacionCafeRequest.Estado));

        var eliminarConStock = await client.DeleteAsync(
            $"/api/v1/inventario/presentaciones/{presentacionId}");
        await AssertValidationProblemAsync(eliminarConStock, "Estado");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            presentacionId, tipoId, 10);

        var eliminarSinStock = await client.DeleteAsync(
            $"/api/v1/inventario/presentaciones/{presentacionId}");
        Assert.Equal(HttpStatusCode.NoContent, eliminarSinStock.StatusCode);

        var presentaciones = await client.GetFromJsonAsync<PresentacionCafeDto[]>(
            "/api/v1/inventario/presentaciones");
        Assert.NotNull(presentaciones);
        Assert.DoesNotContain(presentaciones!, x => x.PresentacionCafeId == presentacionId);

        var movimientoConEliminada = await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            presentacionId, tipoId, 1);
        await AssertValidationProblemAsync(
            movimientoConEliminada, nameof(RegistrarMovimientoRequest.PresentacionCafeId));
    }

    [Fact]
    public async Task TipoCafe_NoSeEliminaConStockYSePuedeEliminarAlAgotarse()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");

        var presentacionId = await factory.SeedPresentacionCafeAsync("250G", "250 g", 250);
        var tipoId = await CreateTipoCafeAsync(client, "GRANO", "Grano");
        var loteId = await CreateLoteAsync(client, "LOTE-TIPO");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Ingreso,
            presentacionId, tipoId, 5);

        var eliminarConStock = await client.DeleteAsync($"/api/v1/inventario/tipos-cafe/{tipoId}");
        await AssertValidationProblemAsync(eliminarConStock, "Estado");

        await RegisterMovementAsync(
            client, loteId, TipoMovimientoLoteCafe.Salida,
            presentacionId, tipoId, 5);

        var eliminarSinStock = await client.DeleteAsync($"/api/v1/inventario/tipos-cafe/{tipoId}");
        Assert.Equal(HttpStatusCode.NoContent, eliminarSinStock.StatusCode);
    }

    [Fact]
    public async Task Catalogos_ValidanCodigosDuplicados()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = await CreateInventoryClientAsync(factory, "inventory-admin@test.local");

        await CreatePresentacionAsync(client, "250G", "250 g", 250);
        var duplicatePresentacion = await client.PostAsJsonAsync(
            "/api/v1/inventario/presentaciones",
            new { codigo = "250G", nombre = "Otro nombre", pesoUnitarioGramos = 250 });
        await AssertValidationProblemAsync(
            duplicatePresentacion, nameof(UpsertPresentacionCafeRequest.Codigo));

        await CreateTipoCafeAsync(client, "GRANO", "Grano");
        var duplicateTipo = await client.PostAsJsonAsync(
            "/api/v1/inventario/tipos-cafe",
            new { codigo = "GRANO", nombre = "Otro tipo" });
        await AssertValidationProblemAsync(
            duplicateTipo, nameof(UpsertTipoCafeRequest.Codigo));
    }

    [Fact]
    public async Task Endpoints_RequierenAutenticacionPermisosYCsfr()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var anonymous = factory.CreateClient();

        var unauthorized = await anonymous.GetAsync("/api/v1/inventario/lotes");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        await factory.SeedUserWithPermissionsAsync(
            "inventory-viewer@test.local",
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver));
        using var viewer = await factory.CreateAuthenticatedClientAsync("inventory-viewer@test.local");

        var list = await viewer.GetAsync("/api/v1/inventario/lotes");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var forbiddenCreate = await viewer.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-SIN-PERMISO",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

        var forbiddenExistencias = await viewer.GetAsync("/api/v1/inventario/lotes/1/existencias");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenExistencias.StatusCode);

        await factory.SeedUserWithPermissionsAsync(
            "inventory-writer@test.local",
            (Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear));
        using var writerWithoutCsrf = await factory.CreateAuthenticatedClientAsync(
            "inventory-writer@test.local",
            csrfTokenOverride: string.Empty);

        var missingCsrf = await writerWithoutCsrf.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-SIN-CSRF",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
        Assert.Equal(HttpStatusCode.Forbidden, missingCsrf.StatusCode);
    }

    private static async Task<HttpClient> CreateInventoryClientAsync(
        ProyectoCafeApiFactory factory,
        string email)
    {
        await factory.SeedUserWithPermissionsAsync(email, AllInventoryPermissions);
        return await factory.CreateAuthenticatedClientAsync(email);
    }

    private static async Task<int> CreateLoteAsync(HttpClient client, string codigoLote)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote,
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoteCreadoResponse>();
        Assert.NotNull(body);
        return body!.LoteCafeId;
    }

    private static async Task<int> CreatePresentacionAsync(
        HttpClient client,
        string codigo,
        string nombre,
        int pesoUnitarioGramos)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventario/presentaciones", new
        {
            codigo,
            nombre,
            pesoUnitarioGramos,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PresentacionCreadaResponse>();
        Assert.NotNull(body);
        return body!.PresentacionCafeId;
    }

    private static async Task<int> CreateTipoCafeAsync(
        HttpClient client,
        string codigo,
        string nombre)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventario/tipos-cafe", new
        {
            codigo,
            nombre,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TipoCreadoResponse>();
        Assert.NotNull(body);
        return body!.TipoCafeId;
    }

    private static Task<HttpResponseMessage> RegisterMovementAsync(
        HttpClient client,
        int loteCafeId,
        TipoMovimientoLoteCafe tipoMovimiento,
        int presentacionCafeId,
        int tipoCafeId,
        int cantidadPaquetes,
        string? observacion = null,
        DateTime? fechaMovimiento = null) =>
        client.PostAsJsonAsync(
            $"/api/v1/inventario/lotes/{loteCafeId}/movimientos",
            new
            {
                tipoMovimiento = (int)tipoMovimiento,
                presentacionCafeId,
                tipoCafeId,
                cantidadPaquetes,
                observacion,
                fechaMovimiento,
            });

    private static async Task<LoteCafeResumenDto[]> GetLotesAsync(HttpClient client)
    {
        var lotes = await client.GetFromJsonAsync<LoteCafeResumenDto[]>("/api/v1/inventario/lotes");
        Assert.NotNull(lotes);
        return lotes!;
    }

    private static async Task<LoteCafeDetalleDto> GetLoteAsync(HttpClient client, int loteCafeId)
    {
        var lote = await client.GetFromJsonAsync<LoteCafeDetalleDto>(
            $"/api/v1/inventario/lotes/{loteCafeId}");
        Assert.NotNull(lote);
        return lote!;
    }

    private static async Task<ExistenciaCafeDto[]> GetExistenciasAsync(
        HttpClient client,
        int loteCafeId)
    {
        var existencias = await client.GetFromJsonAsync<ExistenciaCafeDto[]>(
            $"/api/v1/inventario/lotes/{loteCafeId}/existencias");
        Assert.NotNull(existencias);
        return existencias!;
    }

    private static async Task<MovimientoCafeDto[]> GetMovimientosAsync(
        HttpClient client,
        int loteCafeId)
    {
        var movimientos = await client.GetFromJsonAsync<MovimientoCafeDto[]>(
            $"/api/v1/inventario/lotes/{loteCafeId}/movimientos");
        Assert.NotNull(movimientos);
        return movimientos!;
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedErrorKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(
            problem!.Errors.ContainsKey(expectedErrorKey),
            $"Se esperaba el error '{expectedErrorKey}'. Errores: {string.Join(", ", problem.Errors.Keys)}");
    }

    private sealed class LoteCreadoResponse
    {
        public int LoteCafeId { get; set; }
    }

    private sealed class PresentacionCreadaResponse
    {
        public int PresentacionCafeId { get; set; }
    }

    private sealed class TipoCreadoResponse
    {
        public int TipoCafeId { get; set; }
    }
}
