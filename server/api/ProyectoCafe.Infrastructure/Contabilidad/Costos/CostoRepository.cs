using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Contabilidad.Costos;

public sealed class CostoRepository(ProyectoCafeDbContext dbContext) : ICostoRepository
{
    public async Task<IReadOnlyList<CostoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Costos
            .AsNoTracking()
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.CostoId)
            .Select(x => new CostoResponse
            {
                CostoId = x.CostoId,
                Nombre = x.Nombre,
                TipoCosto = x.TipoCosto,
                FuenteModulo = x.FuenteModulo,
                ReferenciaId = x.ReferenciaId,
                ParcelaId = x.ParcelaId,
                ParcelaNombre = dbContext.Parcelas.Where(p => p.ParcelaId == x.ParcelaId).Select(p => p.Nombre).FirstOrDefault(),
                FincaId = dbContext.Parcelas.Where(p => p.ParcelaId == x.ParcelaId).Select(p => (int?)p.FincaId).FirstOrDefault(),
                FincaNombre = (
                    from parcela in dbContext.Parcelas
                    join finca in dbContext.Fincas on parcela.FincaId equals finca.FincaId
                    where parcela.ParcelaId == x.ParcelaId
                    select finca.Nombre).FirstOrDefault(),
                LoteId = x.LoteId,
                CodigoLote = dbContext.Lotes.Where(l => l.LoteId == x.LoteId).Select(l => l.CodigoLote).FirstOrDefault(),
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CostoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Costos
            .AsNoTracking()
            .Where(x => x.CostoId == id)
            .Select(x => new CostoResponse
            {
                CostoId = x.CostoId,
                Nombre = x.Nombre,
                TipoCosto = x.TipoCosto,
                FuenteModulo = x.FuenteModulo,
                ReferenciaId = x.ReferenciaId,
                ParcelaId = x.ParcelaId,
                ParcelaNombre = dbContext.Parcelas.Where(p => p.ParcelaId == x.ParcelaId).Select(p => p.Nombre).FirstOrDefault(),
                FincaId = dbContext.Parcelas.Where(p => p.ParcelaId == x.ParcelaId).Select(p => (int?)p.FincaId).FirstOrDefault(),
                FincaNombre = (
                    from parcela in dbContext.Parcelas
                    join finca in dbContext.Fincas on parcela.FincaId equals finca.FincaId
                    where parcela.ParcelaId == x.ParcelaId
                    select finca.Nombre).FirstOrDefault(),
                LoteId = x.LoteId,
                CodigoLote = dbContext.Lotes.Where(l => l.LoteId == x.LoteId).Select(l => l.CodigoLote).FirstOrDefault(),
                Monto = x.Monto,
                Fecha = x.Fecha,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CostoRequest request, CancellationToken cancellationToken)
    {
        var nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");

        var entity = new Costo
        {
            Nombre = nombre,
            TipoCosto = TiposCostoContable.Normalize(request.TipoCosto),
            FuenteModulo = NormalizeOptional(request.FuenteModulo),
            ReferenciaId = NormalizeOptionalId(request.ReferenciaId),
            ParcelaId = NormalizeOptionalId(request.ParcelaId),
            LoteId = NormalizeOptionalId(request.LoteId),
            Monto = request.Monto,
            Estado = request.Estado,
        };

        if (request.Fecha.HasValue)
        {
            entity.Fecha = request.Fecha.Value;
        }

        dbContext.Costos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.CostoId;
    }

    public async Task<bool> UpdateAsync(int id, CostoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Costos
            .SingleOrDefaultAsync(x => x.CostoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        if (!IsManual(entity))
        {
            throw new ArgumentException("Los costos generados por operaciones de producción no pueden editarse manualmente.", nameof(id));
        }

        entity.Nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");
        entity.TipoCosto = TiposCostoContable.Normalize(request.TipoCosto);
        entity.FuenteModulo = NormalizeOptional(request.FuenteModulo);
        entity.ReferenciaId = NormalizeOptionalId(request.ReferenciaId);
        entity.ParcelaId = NormalizeOptionalId(request.ParcelaId);
        entity.LoteId = NormalizeOptionalId(request.LoteId);
        entity.Monto = request.Monto;
        entity.Estado = request.Estado;
        if (request.Fecha.HasValue)
        {
            entity.Fecha = request.Fecha.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Costos
            .SingleOrDefaultAsync(x => x.CostoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        if (!IsManual(entity))
        {
            throw new ArgumentException("Los costos generados por operaciones de producción no pueden eliminarse manualmente.", nameof(id));
        }

        dbContext.Costos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        var v = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(v))
        {
            throw new ArgumentException(errorMessage);
        }

        return v;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int? NormalizeOptionalId(int? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value : null;
    }

    private static bool IsManual(Costo entity)
    {
        return entity.TipoCosto.Equals(TiposCostoContable.Manual, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(entity.FuenteModulo);
    }
}
