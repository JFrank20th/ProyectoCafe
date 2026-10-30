using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Application.Services;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Repositories;

namespace ProyectoCafe.Tests;

public sealed class InventarioCafeServiceTests
{
    [Fact]
    public async Task RegistrarMovimiento_CuandoLaPersistenciaReportaConflicto_RetornaConflict()
    {
        var options = new DbContextOptionsBuilder<ProyectoCafeDbContext>()
            .UseInMemoryDatabase($"inventario-conflicto-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ProyectoCafeDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var presentacion = new PresentacionCafe
        {
            Codigo = "250G",
            Nombre = "250 g",
            PesoUnitarioGramos = 250,
            Estado = EstadoCatalogoCafe.Activo,
            CreadoEn = DateTime.UtcNow,
        };
        var tipo = new TipoCafe
        {
            Codigo = "GRANO",
            Nombre = "Grano",
            Estado = EstadoCatalogoCafe.Activo,
            CreadoEn = DateTime.UtcNow,
        };
        var lote = new LoteCafeTerminado
        {
            CodigoLote = "LOTE-CONFLICTO",
            FechaProduccion = DateTime.UtcNow.AddDays(-1),
            Estado = EstadoLoteCafe.Activo,
            CreadoEn = DateTime.UtcNow,
        };

        db.PresentacionesCafe.Add(presentacion);
        db.TiposCafe.Add(tipo);
        db.LotesCafeTerminado.Add(lote);
        await db.SaveChangesAsync();

        var service = new LoteCafeService(
            new LoteCafeRepository(db),
            new CatalogoCafeRepository(db),
            new ConflictUnitOfWork(),
            TimeProvider.System);

        var result = await service.RegisterMovementAsync(
            lote.LoteCafeId,
            new RegistrarMovimientoRequest
            {
                TipoMovimiento = TipoMovimientoLoteCafe.Ingreso,
                PresentacionCafeId = presentacion.PresentacionCafeId,
                TipoCafeId = tipo.TipoCafeId,
                CantidadPaquetes = 1,
            },
            usuarioId: null,
            CancellationToken.None);

        Assert.Equal(OperationStatus.Conflict, result.Status);
        Assert.Null(result.Value);

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.MovimientosLoteCafe.CountAsync());
        Assert.Equal(0, await db.LotesCafeExistencias.CountAsync());
    }

    private sealed class ConflictUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken ct) =>
            Task.FromException(new ConcurrencyConflictException("Conflicto simulado."));

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken ct) =>
            operation(ct);
    }
}
