using System.Net;
using System.Net.Http.Json;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Services;
using ProyectoCafe.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class VentaCancelacionTests
{
    [Fact]
    public async Task CancelVenta_RestoresStockAndMarksCancelled()
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
        await factory.SeedUserWithPermissionsAsync("ventas-cancel@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-cancel@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-CANCEL",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
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
                    cantidadPaquetes = 4,
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var ventaBody = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var ventaId = ventaBody!["ventaId"];
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();

        var ventaInicial = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        var numeroVenta = ventaInicial.Value!.NumeroVenta;

        var movimientosOriginales = await loteRepository.ListMovimientosByReferenciaAsync(
            $"VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.Salida,
            CancellationToken.None);
        Assert.Single(movimientosOriginales);
        var movimientoOriginal = movimientosOriginales[0];
        var existenciaOriginalId = movimientoOriginal.LoteCafeExistenciaId;
        var cantidadOriginal = movimientoOriginal.CantidadPaquetes;
        var pesoOriginal = movimientoOriginal.PesoNetoKg;

        var loteParaAgotar = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        loteParaAgotar!.Estado = EstadoLoteCafe.Agotado;
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        await db.SaveChangesAsync(CancellationToken.None);

        var result = await ventaService.CancelVentaAsync(ventaId, "Cancelaci�n de prueba", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.Equal(EstadoVenta.Cancelada, venta.Value!.Estado);

        var movimientosInversos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"CANCELACION-VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);
        Assert.Single(movimientosInversos);
        var movimientoInverso = movimientosInversos[0];
        Assert.Equal(existenciaOriginalId, movimientoInverso.LoteCafeExistenciaId);
        Assert.Equal(cantidadOriginal, movimientoInverso.CantidadPaquetes);
        Assert.Equal(pesoOriginal, movimientoInverso.PesoNetoKg);
        Assert.True(movimientoInverso.Observacion!.Length <= 250);
        Assert.Equal(4, movimientoInverso.LoteCafeExistencia!.CantidadPaquetes);

        var loteFinal = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        Assert.Equal(EstadoLoteCafe.Activo, loteFinal!.Estado);
    }

    [Fact]
    public async Task AnulateVenta_MarksAnnulled()
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
        await factory.SeedUserWithPermissionsAsync("ventas-anulate@test.local", permissions);
        using var client = await factory.CreateAuthenticatedClientAsync("ventas-anulate@test.local");

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = "LOTE-ANULATE",
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
                    cantidadPaquetes = 2,
                },
            },
        });
        var ventaId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();

        var ventaInicial = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        var numeroVenta = ventaInicial.Value!.NumeroVenta;

        var movimientosOriginales = await loteRepository.ListMovimientosByReferenciaAsync(
            $"VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.Salida,
            CancellationToken.None);
        Assert.Single(movimientosOriginales);
        var movimientoOriginal = movimientosOriginales[0];
        var existenciaOriginalId = movimientoOriginal.LoteCafeExistenciaId;
        var cantidadOriginal = movimientoOriginal.CantidadPaquetes;
        var pesoOriginal = movimientoOriginal.PesoNetoKg;

        var loteParaAgotar = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        loteParaAgotar!.Estado = EstadoLoteCafe.Agotado;
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();
        await db.SaveChangesAsync(CancellationToken.None);

        var result = await ventaService.AnulateVentaAsync(ventaId, "Anulaci�n de prueba", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.Equal(EstadoVenta.Anulada, venta.Value!.Estado);

        var movimientosInversos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"ANULACION-VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);
        Assert.Single(movimientosInversos);
        var movimientoInverso = movimientosInversos[0];
        Assert.Equal(existenciaOriginalId, movimientoInverso.LoteCafeExistenciaId);
        Assert.Equal(cantidadOriginal, movimientoInverso.CantidadPaquetes);
        Assert.Equal(pesoOriginal, movimientoInverso.PesoNetoKg);
        Assert.True(movimientoInverso.Observacion!.Length <= 250);
        Assert.Equal(2, movimientoInverso.LoteCafeExistencia!.CantidadPaquetes);

        var loteFinal = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        Assert.Equal(EstadoLoteCafe.Activo, loteFinal!.Estado);
    }

    [Fact]
    public async Task CancelledSale_RejectsPayment()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-cancel-payment@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var pagoService = scope.ServiceProvider.GetRequiredService<PagoVentaService>();

        await ventaService.CancelVentaAsync(ventaId, "Cancelación", null, CancellationToken.None);

        var result = await pagoService.RegisterPagoAsync(
            ventaId,
            new CreatePagoVentaRequest
            {
                Monto = 1000m,
                MetodoPago = "Efectivo",
            },
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationStatus.ValidationFailed, result.Status);

        var pagos = await pagoService.GetPagosAsync(ventaId, CancellationToken.None);
        Assert.Empty(pagos.Value!);
    }

    [Fact]
    public async Task AnnulledSale_RejectsPayment()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-anulate-payment@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var pagoService = scope.ServiceProvider.GetRequiredService<PagoVentaService>();

        await ventaService.AnulateVentaAsync(ventaId, "Anulación", null, CancellationToken.None);

        var result = await pagoService.RegisterPagoAsync(
            ventaId,
            new CreatePagoVentaRequest
            {
                Monto = 1000m,
                MetodoPago = "Efectivo",
            },
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task CancelledSale_PreservesHistoricalPayments()
    {
        var (factory, ventaId) = await SeedSaleWithPaymentAsync("ventas-cancel-historical@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var pagoService = scope.ServiceProvider.GetRequiredService<PagoVentaService>();

        await ventaService.CancelVentaAsync(ventaId, "Cancelación", null, CancellationToken.None);

        var pagos = await pagoService.GetPagosAsync(ventaId, CancellationToken.None);
        Assert.Single(pagos.Value!);
        Assert.Equal(20000m, pagos.Value![0].Monto);
    }

    [Fact]
    public async Task AnnulledSale_PreservesHistoricalPayments()
    {
        var (factory, ventaId) = await SeedSaleWithPaymentAsync("ventas-anulate-historical@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var pagoService = scope.ServiceProvider.GetRequiredService<PagoVentaService>();

        await ventaService.AnulateVentaAsync(ventaId, "Anulación", null, CancellationToken.None);

        var pagos = await pagoService.GetPagosAsync(ventaId, CancellationToken.None);
        Assert.Single(pagos.Value!);
    }

    [Fact]
    public async Task CancelledSale_RejectsDeliveryUpdate()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-cancel-delivery@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();

        await ventaService.CancelVentaAsync(ventaId, "Cancelación", null, CancellationToken.None);

        var result = await ventaService.UpdateEstadoEntregaAsync(
            ventaId,
            EstadoEntregaVenta.EnPreparacion,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationStatus.ValidationFailed, result.Status);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.Equal(EstadoEntregaVenta.PendientePreparacion, venta.Value!.EstadoEntrega);
    }

    [Fact]
    public async Task AnnulledSale_RejectsDeliveryUpdate()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-anulate-delivery@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();

        await ventaService.AnulateVentaAsync(ventaId, "Anulación", null, CancellationToken.None);

        var result = await ventaService.UpdateEstadoEntregaAsync(
            ventaId,
            EstadoEntregaVenta.EnPreparacion,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task CancelVenta_With250CharacterReason_PersistsFullReasonAndBoundedObservation()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-cancel-250@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();

        var motivo = new string('A', 250);
        var result = await ventaService.CancelVentaAsync(ventaId, motivo, null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.NotNull(venta.Value);
        Assert.Equal(motivo, venta.Value!.MotivoCancelacion);
        Assert.Equal(250, venta.Value.MotivoCancelacion!.Length);

        var movimientos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"CANCELACION-VENTA-{venta.Value.NumeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);

        Assert.Single(movimientos);
        Assert.NotNull(movimientos[0].Observacion);
        Assert.True(movimientos[0].Observacion!.Length <= 250);
        Assert.Equal(4, movimientos[0].LoteCafeExistencia!.CantidadPaquetes);
    }

    [Fact]
    public async Task AnulateVenta_With250CharacterReason_PersistsFullReasonAndBoundedObservation()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-anulate-250@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();

        var motivo = new string('B', 250);
        var result = await ventaService.AnulateVentaAsync(ventaId, motivo, null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.NotNull(venta.Value);
        Assert.Equal(motivo, venta.Value!.MotivoAnulacion);
        Assert.Equal(250, venta.Value.MotivoAnulacion!.Length);

        var movimientos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"ANULACION-VENTA-{venta.Value.NumeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);

        Assert.Single(movimientos);
        Assert.NotNull(movimientos[0].Observacion);
        Assert.True(movimientos[0].Observacion!.Length <= 250);
        Assert.Equal(4, movimientos[0].LoteCafeExistencia!.CantidadPaquetes);
    }


    [Fact]
    public async Task CancelVenta_BlockedLote_PreservesBlockedState()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-cancel-blocked@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var ventaInicial = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        var numeroVenta = ventaInicial.Value!.NumeroVenta;

        var movimientosOriginales = await loteRepository.ListMovimientosByReferenciaAsync(
            $"VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.Salida,
            CancellationToken.None);
        Assert.Single(movimientosOriginales);
        var movimientoOriginal = movimientosOriginales[0];
        var existenciaOriginalId = movimientoOriginal.LoteCafeExistenciaId;
        var loteId = movimientoOriginal.LoteCafeExistencia!.LoteCafeId;

        var lote = await db.LotesCafeTerminado.FirstAsync(l => l.LoteCafeId == loteId, CancellationToken.None);
        lote.Estado = EstadoLoteCafe.Bloqueado;
        await db.SaveChangesAsync(CancellationToken.None);

        var result = await ventaService.CancelVentaAsync(ventaId, "Cancelaci�n bloqueada", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.Equal(EstadoVenta.Cancelada, venta.Value!.Estado);

        var existenciaRestaurada = await db.LotesCafeExistencias.FindAsync(existenciaOriginalId);
        Assert.Equal(4, existenciaRestaurada!.CantidadPaquetes);

        var movimientosInversos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"CANCELACION-VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);
        Assert.Single(movimientosInversos);
        Assert.Equal(existenciaOriginalId, movimientosInversos[0].LoteCafeExistenciaId);

        var loteFinal = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        Assert.Equal(EstadoLoteCafe.Bloqueado, loteFinal!.Estado);
    }

    [Fact]
    public async Task AnulateVenta_BlockedLote_PreservesBlockedState()
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync("ventas-anulate-blocked@test.local");
        using var scope = factory.Services.CreateScope();
        var ventaService = scope.ServiceProvider.GetRequiredService<VentaService>();
        var loteRepository = scope.ServiceProvider.GetRequiredService<ILoteCafeRepository>();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        var ventaInicial = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        var numeroVenta = ventaInicial.Value!.NumeroVenta;

        var movimientosOriginales = await loteRepository.ListMovimientosByReferenciaAsync(
            $"VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.Salida,
            CancellationToken.None);
        Assert.Single(movimientosOriginales);
        var movimientoOriginal = movimientosOriginales[0];
        var existenciaOriginalId = movimientoOriginal.LoteCafeExistenciaId;
        var loteId = movimientoOriginal.LoteCafeExistencia!.LoteCafeId;

        var lote = await db.LotesCafeTerminado.FirstAsync(l => l.LoteCafeId == loteId, CancellationToken.None);
        lote.Estado = EstadoLoteCafe.Bloqueado;
        await db.SaveChangesAsync(CancellationToken.None);

        var result = await ventaService.AnulateVentaAsync(ventaId, "Anulaci�n bloqueada", null, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var venta = await ventaService.GetVentaAsync(ventaId, CancellationToken.None);
        Assert.Equal(EstadoVenta.Anulada, venta.Value!.Estado);

        var existenciaRestaurada = await db.LotesCafeExistencias.FindAsync(existenciaOriginalId);
        Assert.Equal(4, existenciaRestaurada!.CantidadPaquetes);

        var movimientosInversos = await loteRepository.ListMovimientosByReferenciaAsync(
            $"ANULACION-VENTA-{numeroVenta}",
            TipoMovimientoLoteCafe.AjustePositivo,
            CancellationToken.None);
        Assert.Single(movimientosInversos);
        Assert.Equal(existenciaOriginalId, movimientosInversos[0].LoteCafeExistenciaId);

        var loteFinal = await loteRepository.FindLoteAsync(loteId, CancellationToken.None);
        Assert.Equal(EstadoLoteCafe.Bloqueado, loteFinal!.Estado);
    }


    private static async Task<(ProyectoCafeApiFactory Factory, int VentaId)> SeedSaleForCancellationAsync(string email)
    {
        var factory = new ProyectoCafeApiFactory();
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
        await factory.SeedUserWithPermissionsAsync(email, permissions);
        using var client = await factory.CreateAuthenticatedClientAsync(email);

        var catalogs = await factory.SeedInventarioCafeCatalogsAsync();

        var loteResponse = await client.PostAsJsonAsync("/api/v1/inventario/lotes", new
        {
            codigoLote = $"LOTE-{email}",
            fechaProduccion = DateTime.UtcNow.AddDays(-1),
        });
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
                    cantidadPaquetes = 2,
                },
            },
        });
        var ventaId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["ventaId"];

        return (factory, ventaId);
    }

    private static async Task<(ProyectoCafeApiFactory Factory, int VentaId)> SeedSaleWithPaymentAsync(string email)
    {
        var (factory, ventaId) = await SeedSaleForCancellationAsync(email);
        using var scope = factory.Services.CreateScope();
        var pagoService = scope.ServiceProvider.GetRequiredService<PagoVentaService>();

        await pagoService.RegisterPagoAsync(
            ventaId,
            new CreatePagoVentaRequest
            {
                Monto = 20000m,
                MetodoPago = "Efectivo",
            },
            null,
            CancellationToken.None);

        return (factory, ventaId);
    }

    private sealed record ExistenciaResponse(int CantidadPaquetes);
}