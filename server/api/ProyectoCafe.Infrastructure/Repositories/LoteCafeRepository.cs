using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class LoteCafeRepository(ProyectoCafeDbContext db) : ILoteCafeRepository
{
    private readonly ProyectoCafeDbContext _db = db;

    public Task<bool> CodigoExistsAsync(string codigoLote, CancellationToken ct) =>
        _db.LotesCafeTerminado.AnyAsync(l => l.CodigoLote == codigoLote, ct);

    public Task AddLoteAsync(LoteCafeTerminado lote, CancellationToken ct)
    {
        _db.LotesCafeTerminado.Add(lote);
        return Task.CompletedTask;
    }

    public Task<LoteCafeTerminado?> FindLoteAsync(int loteCafeId, CancellationToken ct) =>
        _db.LotesCafeTerminado.FirstOrDefaultAsync(
            l => l.LoteCafeId == loteCafeId && l.Estado != EstadoLoteCafe.Eliminado,
            ct);

    public async Task<IReadOnlyList<LoteCafeResumenRow>> ListLotesAsync(CancellationToken ct) =>
        await _db.LotesCafeTerminado
            .AsNoTracking()
            .Where(l => l.Estado != EstadoLoteCafe.Eliminado)
            .OrderByDescending(l => l.FechaProduccion)
            .ThenByDescending(l => l.LoteCafeId)
            .Select(l => new LoteCafeResumenRow
            {
                LoteCafeId = l.LoteCafeId,
                CodigoLote = l.CodigoLote,
                FechaProduccion = l.FechaProduccion,
                FechaEnvasado = l.FechaEnvasado,
                FechaVencimiento = l.FechaVencimiento,
                Estado = l.Estado,
                Observacion = l.Observacion,
                CreadoEn = l.CreadoEn,
                ActualizadoEn = l.ActualizadoEn,
                VariedadCafeId = l.VariedadCafeId,
                TipoTuesteCafeId = l.TipoTuesteCafeId,
                TipoProcesoCafeId = l.TipoProcesoCafeId,
                TotalPaquetes = l.Existencias.Sum(e => (int?)e.CantidadPaquetes) ?? 0,
                TotalPesoNetoKg = l.Existencias.Sum(
                    e => (decimal?)((decimal)e.CantidadPaquetes * e.PesoUnitarioGramos / 1000m)) ?? 0m,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExistenciaCafeRow>> ListExistenciasAsync(
        int loteCafeId,
        CancellationToken ct) =>
        await _db.LotesCafeExistencias
            .AsNoTracking()
            .Where(e => e.LoteCafeId == loteCafeId)
            .OrderBy(e => e.PresentacionCafe!.Nombre)
            .ThenBy(e => e.TipoCafe!.Nombre)
            .Select(e => new ExistenciaCafeRow
            {
                LoteCafeExistenciaId = e.LoteCafeExistenciaId,
                LoteCafeId = e.LoteCafeId,
                PresentacionCafeId = e.PresentacionCafeId,
                PresentacionCodigo = e.PresentacionCafe!.Codigo,
                PresentacionNombre = e.PresentacionCafe!.Nombre,
                PresentacionEstado = e.PresentacionCafe!.Estado,
                TipoCafeId = e.TipoCafeId,
                TipoCafeCodigo = e.TipoCafe!.Codigo,
                TipoCafeNombre = e.TipoCafe!.Nombre,
                TipoCafeEstado = e.TipoCafe!.Estado,
                CantidadPaquetes = e.CantidadPaquetes,
                PesoUnitarioGramos = e.PesoUnitarioGramos,
                ActualizadoEn = e.ActualizadoEn,
            })
            .ToListAsync(ct);

    public Task<LoteCafeExistencia?> FindExistenciaAsync(
        int loteCafeId,
        int presentacionCafeId,
        int tipoCafeId,
        CancellationToken ct) =>
        _db.LotesCafeExistencias.FirstOrDefaultAsync(
            e => e.LoteCafeId == loteCafeId &&
                 e.PresentacionCafeId == presentacionCafeId &&
                 e.TipoCafeId == tipoCafeId,
            ct);

    public Task AddExistenciaAsync(LoteCafeExistencia existencia, CancellationToken ct)
    {
        _db.LotesCafeExistencias.Add(existencia);
        return Task.CompletedTask;
    }

    public Task AddMovimientoAsync(MovimientoLoteCafe movimiento, CancellationToken ct)
    {
        _db.MovimientosLoteCafe.Add(movimiento);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MovimientoCafeRow>> ListMovimientosAsync(
        int loteCafeId,
        CancellationToken ct) =>
        await _db.MovimientosLoteCafe
            .AsNoTracking()
            .Where(m => m.LoteCafeExistencia!.LoteCafeId == loteCafeId)
            .OrderByDescending(m => m.FechaMovimiento)
            .ThenByDescending(m => m.MovimientoLoteCafeId)
            .Select(m => new MovimientoCafeRow
            {
                MovimientoLoteCafeId = m.MovimientoLoteCafeId,
                LoteCafeExistenciaId = m.LoteCafeExistenciaId,
                LoteCafeId = m.LoteCafeExistencia!.LoteCafeId,
                TipoMovimiento = m.TipoMovimiento,
                CantidadPaquetes = m.CantidadPaquetes,
                PesoNetoKg = m.PesoNetoKg,
                FechaMovimiento = m.FechaMovimiento,
                Referencia = m.Referencia,
                Observacion = m.Observacion,
                UsuarioId = m.UsuarioId,
                CreadoEn = m.CreadoEn,
                PresentacionCafeId = m.LoteCafeExistencia!.PresentacionCafeId,
                PresentacionCodigo = m.LoteCafeExistencia!.PresentacionCafe!.Codigo,
                PresentacionNombre = m.LoteCafeExistencia!.PresentacionCafe!.Nombre,
                TipoCafeId = m.LoteCafeExistencia!.TipoCafeId,
                TipoCafeCodigo = m.LoteCafeExistencia!.TipoCafe!.Codigo,
                TipoCafeNombre = m.LoteCafeExistencia!.TipoCafe!.Nombre,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MovimientoLoteCafe>> ListMovimientosByReferenciaAsync(
        string referencia,
        TipoMovimientoLoteCafe? tipoMovimiento = null,
        CancellationToken ct = default) =>
        await _db.MovimientosLoteCafe
            .Include(m => m.LoteCafeExistencia!)
                .ThenInclude(e => e.LoteCafe!)
            .Include(m => m.LoteCafeExistencia!)
                .ThenInclude(e => e.PresentacionCafe!)
            .Include(m => m.LoteCafeExistencia!)
                .ThenInclude(e => e.TipoCafe!)
            .Where(m => m.Referencia == referencia &&
                        (!tipoMovimiento.HasValue || m.TipoMovimiento == tipoMovimiento.Value))
            .OrderBy(m => m.MovimientoLoteCafeId)
            .ToListAsync(ct);

    public async Task<int> GetTotalPaquetesAsync(int loteCafeId, CancellationToken ct) =>
        await _db.LotesCafeExistencias
            .Where(e => e.LoteCafeId == loteCafeId)
            .SumAsync(e => (int?)e.CantidadPaquetes, ct) ?? 0;
}
