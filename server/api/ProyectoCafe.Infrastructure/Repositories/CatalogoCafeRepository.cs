using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class CatalogoCafeRepository(ProyectoCafeDbContext db) : ICatalogoCafeRepository
{
    private readonly ProyectoCafeDbContext _db = db;

    public async Task<IReadOnlyList<PresentacionCafe>> ListPresentacionesAsync(CancellationToken ct) =>
        await _db.PresentacionesCafe
            .AsNoTracking()
            .Where(p => p.Estado != EstadoCatalogoCafe.Eliminado)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

    public Task<PresentacionCafe?> FindPresentacionAsync(
        int presentacionCafeId,
        CancellationToken ct) =>
        _db.PresentacionesCafe.FirstOrDefaultAsync(
            p => p.PresentacionCafeId == presentacionCafeId &&
                 p.Estado != EstadoCatalogoCafe.Eliminado,
            ct);

    public Task<bool> PresentacionCodigoExistsAsync(
        string codigo,
        int? excludePresentacionCafeId,
        CancellationToken ct) =>
        _db.PresentacionesCafe.AnyAsync(
            p => p.Codigo == codigo &&
                 (!excludePresentacionCafeId.HasValue ||
                  p.PresentacionCafeId != excludePresentacionCafeId.Value),
            ct);

    public Task<bool> PresentacionNombreExistsAsync(
        string nombre,
        int? excludePresentacionCafeId,
        CancellationToken ct) =>
        _db.PresentacionesCafe.AnyAsync(
            p => p.Nombre == nombre &&
                 (!excludePresentacionCafeId.HasValue ||
                  p.PresentacionCafeId != excludePresentacionCafeId.Value),
            ct);

    public Task AddPresentacionAsync(PresentacionCafe presentacion, CancellationToken ct)
    {
        _db.PresentacionesCafe.Add(presentacion);
        return Task.CompletedTask;
    }

    public Task<bool> PresentacionHasExistenciasAsync(
        int presentacionCafeId,
        CancellationToken ct) =>
        _db.LotesCafeExistencias.AnyAsync(
            e => e.PresentacionCafeId == presentacionCafeId,
            ct);

    public Task<bool> PresentacionHasStockAsync(int presentacionCafeId, CancellationToken ct) =>
        _db.LotesCafeExistencias.AnyAsync(
            e => e.PresentacionCafeId == presentacionCafeId && e.CantidadPaquetes > 0,
            ct);

    public async Task<IReadOnlyList<TipoCafe>> ListTiposCafeAsync(CancellationToken ct) =>
        await _db.TiposCafe
            .AsNoTracking()
            .Where(t => t.Estado != EstadoCatalogoCafe.Eliminado)
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);

    public Task<TipoCafe?> FindTipoCafeAsync(int tipoCafeId, CancellationToken ct) =>
        _db.TiposCafe.FirstOrDefaultAsync(
            t => t.TipoCafeId == tipoCafeId && t.Estado != EstadoCatalogoCafe.Eliminado,
            ct);

    public Task<bool> TipoCafeCodigoExistsAsync(
        string codigo,
        int? excludeTipoCafeId,
        CancellationToken ct) =>
        _db.TiposCafe.AnyAsync(
            t => t.Codigo == codigo &&
                 (!excludeTipoCafeId.HasValue || t.TipoCafeId != excludeTipoCafeId.Value),
            ct);

    public Task<bool> TipoCafeNombreExistsAsync(
        string nombre,
        int? excludeTipoCafeId,
        CancellationToken ct) =>
        _db.TiposCafe.AnyAsync(
            t => t.Nombre == nombre &&
                 (!excludeTipoCafeId.HasValue || t.TipoCafeId != excludeTipoCafeId.Value),
            ct);

    public Task AddTipoCafeAsync(TipoCafe tipoCafe, CancellationToken ct)
    {
        _db.TiposCafe.Add(tipoCafe);
        return Task.CompletedTask;
    }

    public Task<bool> TipoCafeHasStockAsync(int tipoCafeId, CancellationToken ct) =>
        _db.LotesCafeExistencias.AnyAsync(
            e => e.TipoCafeId == tipoCafeId && e.CantidadPaquetes > 0,
            ct);

    public async Task<IReadOnlyList<VariedadCafe>> ListVariedadesAsync(CancellationToken ct) =>
        await _db.VariedadesCafe
            .AsNoTracking()
            .Where(v => v.Estado != EstadoCatalogoCafe.Eliminado)
            .OrderBy(v => v.Nombre)
            .ToListAsync(ct);

    public Task<VariedadCafe?> FindVariedadAsync(int variedadCafeId, CancellationToken ct) =>
        _db.VariedadesCafe.FirstOrDefaultAsync(
            v => v.VariedadCafeId == variedadCafeId && v.Estado != EstadoCatalogoCafe.Eliminado,
            ct);

    public Task<bool> VariedadCodigoExistsAsync(
        string codigo,
        int? excludeVariedadCafeId,
        CancellationToken ct) =>
        _db.VariedadesCafe.AnyAsync(
            v => v.Codigo == codigo &&
                 (!excludeVariedadCafeId.HasValue || v.VariedadCafeId != excludeVariedadCafeId.Value),
            ct);

    public Task<bool> VariedadNombreExistsAsync(
        string nombre,
        int? excludeVariedadCafeId,
        CancellationToken ct) =>
        _db.VariedadesCafe.AnyAsync(
            v => v.Nombre == nombre &&
                 (!excludeVariedadCafeId.HasValue || v.VariedadCafeId != excludeVariedadCafeId.Value),
            ct);

    public Task AddVariedadAsync(VariedadCafe variedadCafe, CancellationToken ct)
    {
        _db.VariedadesCafe.Add(variedadCafe);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TipoTuesteCafe>> ListTiposTuesteAsync(CancellationToken ct) =>
        await _db.TiposTuesteCafe
            .AsNoTracking()
            .Where(t => t.Estado != EstadoCatalogoCafe.Eliminado)
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);

    public Task<TipoTuesteCafe?> FindTipoTuesteAsync(int tipoTuesteCafeId, CancellationToken ct) =>
        _db.TiposTuesteCafe.FirstOrDefaultAsync(
            t => t.TipoTuesteCafeId == tipoTuesteCafeId && t.Estado != EstadoCatalogoCafe.Eliminado,
            ct);

    public Task<bool> TipoTuesteCodigoExistsAsync(
        string codigo,
        int? excludeTipoTuesteCafeId,
        CancellationToken ct) =>
        _db.TiposTuesteCafe.AnyAsync(
            t => t.Codigo == codigo &&
                 (!excludeTipoTuesteCafeId.HasValue || t.TipoTuesteCafeId != excludeTipoTuesteCafeId.Value),
            ct);

    public Task<bool> TipoTuesteNombreExistsAsync(
        string nombre,
        int? excludeTipoTuesteCafeId,
        CancellationToken ct) =>
        _db.TiposTuesteCafe.AnyAsync(
            t => t.Nombre == nombre &&
                 (!excludeTipoTuesteCafeId.HasValue || t.TipoTuesteCafeId != excludeTipoTuesteCafeId.Value),
            ct);

    public Task AddTipoTuesteAsync(TipoTuesteCafe tipoTuesteCafe, CancellationToken ct)
    {
        _db.TiposTuesteCafe.Add(tipoTuesteCafe);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TipoProcesoCafe>> ListTiposProcesoAsync(CancellationToken ct) =>
        await _db.TiposProcesoCafe
            .AsNoTracking()
            .Where(t => t.Estado != EstadoCatalogoCafe.Eliminado)
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);

    public Task<TipoProcesoCafe?> FindTipoProcesoAsync(int tipoProcesoCafeId, CancellationToken ct) =>
        _db.TiposProcesoCafe.FirstOrDefaultAsync(
            t => t.TipoProcesoCafeId == tipoProcesoCafeId && t.Estado != EstadoCatalogoCafe.Eliminado,
            ct);

    public Task<bool> TipoProcesoCodigoExistsAsync(
        string codigo,
        int? excludeTipoProcesoCafeId,
        CancellationToken ct) =>
        _db.TiposProcesoCafe.AnyAsync(
            t => t.Codigo == codigo &&
                 (!excludeTipoProcesoCafeId.HasValue || t.TipoProcesoCafeId != excludeTipoProcesoCafeId.Value),
            ct);

    public Task<bool> TipoProcesoNombreExistsAsync(
        string nombre,
        int? excludeTipoProcesoCafeId,
        CancellationToken ct) =>
        _db.TiposProcesoCafe.AnyAsync(
            t => t.Nombre == nombre &&
                 (!excludeTipoProcesoCafeId.HasValue || t.TipoProcesoCafeId != excludeTipoProcesoCafeId.Value),
            ct);

    public Task AddTipoProcesoAsync(TipoProcesoCafe tipoProcesoCafe, CancellationToken ct)
    {
        _db.TiposProcesoCafe.Add(tipoProcesoCafe);
        return Task.CompletedTask;
    }
}
