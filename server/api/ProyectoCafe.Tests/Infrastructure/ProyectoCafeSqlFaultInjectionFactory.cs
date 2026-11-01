using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Repositories;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests.Infrastructure;

/// <summary>
/// Factory SQL Server que inyecta un repositorio de inventario que falla
/// de forma controlada para validar rollback transaccional.
/// </summary>
public sealed class ProyectoCafeSqlFaultInjectionFactory : ProyectoCafeSqlIntegrationFactory
{
    public ProyectoCafeSqlFaultInjectionFactory()
    {
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILoteCafeRepository>();
            services.AddScoped<ILoteCafeRepository>(sp =>
                new ThrowingLoteCafeRepository(
                    sp.GetRequiredService<ProyectoCafe.Infrastructure.Data.ProyectoCafeDbContext>()));
        });
    }
}

public sealed class ThrowingLoteCafeRepository : ILoteCafeRepository
{
    private readonly LoteCafeRepository _inner;

    public ThrowingLoteCafeRepository(ProyectoCafe.Infrastructure.Data.ProyectoCafeDbContext db)
    {
        _inner = new LoteCafeRepository(db);
    }

    public Task<bool> CodigoExistsAsync(string codigoLote, CancellationToken ct) =>
        _inner.CodigoExistsAsync(codigoLote, ct);

    public Task AddLoteAsync(LoteCafeTerminado lote, CancellationToken ct) =>
        _inner.AddLoteAsync(lote, ct);

    public Task<LoteCafeTerminado?> FindLoteAsync(int loteCafeId, CancellationToken ct) =>
        _inner.FindLoteAsync(loteCafeId, ct);

    public Task<IReadOnlyList<LoteCafeResumenRow>> ListLotesAsync(CancellationToken ct) =>
        _inner.ListLotesAsync(ct);

    public Task<IReadOnlyList<ExistenciaCafeRow>> ListExistenciasAsync(int loteCafeId, CancellationToken ct) =>
        _inner.ListExistenciasAsync(loteCafeId, ct);

    public Task<LoteCafeExistencia?> FindExistenciaAsync(int loteCafeId, int presentacionCafeId, int tipoCafeId, CancellationToken ct) =>
        _inner.FindExistenciaAsync(loteCafeId, presentacionCafeId, tipoCafeId, ct);

    public Task AddExistenciaAsync(LoteCafeExistencia existencia, CancellationToken ct) =>
        _inner.AddExistenciaAsync(existencia, ct);

    public Task AddMovimientoAsync(MovimientoLoteCafe movimiento, CancellationToken ct)
    {
        if (movimiento.Referencia is not null &&
            (movimiento.Referencia.StartsWith("CANCELACION-", StringComparison.Ordinal) ||
             movimiento.Referencia.StartsWith("ANULACION-", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Error forzado durante la reversión.");
        }

        return _inner.AddMovimientoAsync(movimiento, ct);
    }

    public Task<IReadOnlyList<MovimientoCafeRow>> ListMovimientosAsync(int loteCafeId, CancellationToken ct) =>
        _inner.ListMovimientosAsync(loteCafeId, ct);

    public Task<IReadOnlyList<MovimientoLoteCafe>> ListMovimientosByReferenciaAsync(
        string referencia,
        TipoMovimientoLoteCafe? tipoMovimiento = null,
        CancellationToken ct = default) =>
        _inner.ListMovimientosByReferenciaAsync(referencia, tipoMovimiento, ct);

    public Task<int> GetTotalPaquetesAsync(int loteCafeId, CancellationToken ct) =>
        _inner.GetTotalPaquetesAsync(loteCafeId, ct);
}