using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Produccion.Cultivos;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Produccion.Cultivos;

public sealed class CultivoRepository(ProyectoCafeDbContext dbContext) : ICultivoRepository
{
    public async Task<IReadOnlyList<CultivoResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken)
    {
        var query =
            from cultivo in dbContext.Cultivos.AsNoTracking()
            join finca in dbContext.Fincas.AsNoTracking() on cultivo.FincaId equals finca.FincaId
            select new CultivoResponse
            {
                CultivoId = cultivo.CultivoId,
                FincaId = cultivo.FincaId,
                FincaNombre = finca.Nombre,
                Nombre = cultivo.Nombre,
                Descripcion = cultivo.Descripcion,
                AreaHectareas = cultivo.AreaHectareas,
                Estado = cultivo.Estado ?? true,
            };

        if (fincaId.HasValue)
        {
            query = query.Where(x => x.FincaId == fincaId.Value);
        }

        var cultivos = await query
            .OrderBy(x => x.FincaNombre)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        await AttachHarvestConfigurationsAsync(cultivos, cancellationToken);
        return cultivos;
    }

    public async Task<CultivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var cultivo = await (
            from cultivoEntity in dbContext.Cultivos.AsNoTracking()
            join finca in dbContext.Fincas.AsNoTracking() on cultivoEntity.FincaId equals finca.FincaId
            where cultivoEntity.CultivoId == id
            select new CultivoResponse
            {
                CultivoId = cultivoEntity.CultivoId,
                FincaId = cultivoEntity.FincaId,
                FincaNombre = finca.Nombre,
                Nombre = cultivoEntity.Nombre,
                Descripcion = cultivoEntity.Descripcion,
                AreaHectareas = cultivoEntity.AreaHectareas,
                Estado = cultivoEntity.Estado ?? true,
            }
        )
            .SingleOrDefaultAsync(cancellationToken);

        if (cultivo is null)
        {
            return null;
        }

        await AttachHarvestConfigurationsAsync([cultivo], cancellationToken);
        return cultivo;
    }

    public async Task<int> CreateAsync(CultivoRequest request, CancellationToken cancellationToken)
    {
        var existsFinca = await dbContext.Fincas
            .AnyAsync(x => x.FincaId == request.FincaId, cancellationToken);

        if (!existsFinca)
        {
            throw new ArgumentException("FincaId inválido.", nameof(request));
        }

        var configuraciones = await NormalizeHarvestConfigurationsAsync(request, cancellationToken);

        var entity = new Cultivo
        {
            FincaId = request.FincaId,
            Nombre = NormalizeRequiredText(request.Nombre),
            Descripcion = NormalizeRequiredText(request.Descripcion),
            AreaHectareas = request.AreaHectareas,
            Estado = request.Estado,
            ConfiguracionesCosecha = configuraciones,
        };

        dbContext.Cultivos.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.CultivoId;
    }

    public async Task<bool> UpdateAsync(int id, CultivoRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Cultivos
            .SingleOrDefaultAsync(x => x.CultivoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var existsFinca = await dbContext.Fincas
            .AnyAsync(x => x.FincaId == request.FincaId, cancellationToken);

        if (!existsFinca)
        {
            throw new ArgumentException("FincaId inválido.", nameof(request));
        }

        var configuraciones = await NormalizeHarvestConfigurationsAsync(request, cancellationToken);

        entity.FincaId = request.FincaId;
    entity.ProductoCosechaId = null;
        entity.Nombre = NormalizeRequiredText(request.Nombre);
        entity.Descripcion = NormalizeRequiredText(request.Descripcion);
        entity.AreaHectareas = request.AreaHectareas;
        entity.Estado = request.Estado;

        var existentes = await dbContext.CultivosConfiguracionesCosecha
            .Where(x => x.CultivoId == id)
            .ToListAsync(cancellationToken);

        if (existentes.Count > 0)
        {
            dbContext.CultivosConfiguracionesCosecha.RemoveRange(existentes);
        }

        foreach (var configuracion in configuraciones)
        {
            configuracion.CultivoId = id;
        }

        dbContext.CultivosConfiguracionesCosecha.AddRange(configuraciones);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Cultivos
            .SingleOrDefaultAsync(x => x.CultivoId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var blockedBy = await GetDeleteDependenciesAsync(id, cancellationToken);
        if (blockedBy.Count > 0)
        {
            throw new ArgumentException($"No se puede eliminar el cultivo porque tiene registros asociados en {string.Join(", ", blockedBy)}.");
        }

        dbContext.Cultivos.Remove(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            throw new ArgumentException("No se puede eliminar el cultivo porque tiene registros asociados.", ex);
        }

        return true;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeRequiredText(string? value)
    {
        return NullIfWhiteSpace(value) ?? string.Empty;
    }

    private static string NormalizeHarvestTypeName(string? value)
    {
        return NullIfWhiteSpace(value) ?? string.Empty;
    }

    private async Task<List<CultivoConfiguracionCosecha>> NormalizeHarvestConfigurationsAsync(CultivoRequest request, CancellationToken cancellationToken)
    {
        var requestedItems = request.ConfiguracionesCosecha?
            .Where(x => x is not null)
            .ToList() ?? [];

        if (requestedItems.Count == 0)
        {
            throw new ArgumentException("Debes configurar al menos un tipo de cosecha.", nameof(request.ConfiguracionesCosecha));
        }

        var normalized = new List<CultivoConfiguracionCosecha>(requestedItems.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in requestedItems)
        {
            var nombreTipoCosecha = NormalizeHarvestTypeName(item.NombreTipoCosecha);
            if (string.IsNullOrWhiteSpace(nombreTipoCosecha))
            {
                throw new ArgumentException("NombreTipoCosecha es requerido en cada configuración de cosecha.", nameof(request.ConfiguracionesCosecha));
            }

            var unidadCodigo = NormalizeUnitCode(item.UnidadMedidaCodigo);
            if (string.IsNullOrWhiteSpace(unidadCodigo))
            {
                throw new ArgumentException("UnidadMedidaCodigo es requerido en la configuración de cosecha.", nameof(request.ConfiguracionesCosecha));
            }

            var unidadExiste = await dbContext.UnidadesMedida
                .AsNoTracking()
                .AnyAsync(x => x.Codigo == unidadCodigo && x.Estado != false, cancellationToken);

            if (!unidadExiste)
            {
                throw new ArgumentException($"UnidadMedidaCodigo {unidadCodigo} inválido o inactivo.", nameof(request.ConfiguracionesCosecha));
            }

            var duplicateKey = $"{nombreTipoCosecha}:{unidadCodigo}";
            if (!seen.Add(duplicateKey))
            {
                throw new ArgumentException("No se permiten configuraciones repetidas del mismo tipo de cosecha y unidad en un cultivo.", nameof(request.ConfiguracionesCosecha));
            }

            normalized.Add(new CultivoConfiguracionCosecha
            {
                NombreTipoCosecha = nombreTipoCosecha,
                UnidadMedidaCodigo = unidadCodigo,
            });
        }

        return normalized;
    }

    private async Task AttachHarvestConfigurationsAsync(List<CultivoResponse> cultivos, CancellationToken cancellationToken)
    {
        if (cultivos.Count == 0)
        {
            return;
        }

        var cultivoIds = cultivos.Select(x => x.CultivoId).ToList();
        var configuraciones = await (
            from configuracion in dbContext.CultivosConfiguracionesCosecha.AsNoTracking()
            join unidad in dbContext.UnidadesMedida.AsNoTracking() on configuracion.UnidadMedidaCodigo equals unidad.Codigo
            where cultivoIds.Contains(configuracion.CultivoId)
            orderby configuracion.CultivoId, configuracion.NombreTipoCosecha, configuracion.CultivoConfiguracionCosechaId
            select new
            {
                configuracion.CultivoId,
                Response = new CultivoConfiguracionCosechaResponse
                {
                    CultivoConfiguracionCosechaId = configuracion.CultivoConfiguracionCosechaId,
                    NombreTipoCosecha = configuracion.NombreTipoCosecha,
                    UnidadMedidaCodigo = unidad.Codigo,
                    UnidadMedidaNombre = unidad.Nombre,
                },
            })
            .ToListAsync(cancellationToken);

        var byCultivo = configuraciones
            .GroupBy(x => x.CultivoId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CultivoConfiguracionCosechaResponse>)x.Select(y => y.Response).ToList());

        foreach (var cultivo in cultivos)
        {
            cultivo.ConfiguracionesCosecha = byCultivo.TryGetValue(cultivo.CultivoId, out var items) ? items : [];
        }
    }

    private static string? NormalizeUnitCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task<List<string>> GetDeleteDependenciesAsync(int cultivoId, CancellationToken cancellationToken)
    {
        var blockedBy = new List<string>();

        if (await dbContext.OperacionesAgricolas.AnyAsync(x => x.CultivoId == cultivoId, cancellationToken))
        {
            blockedBy.Add("operaciones agrícolas");
        }

        if (await dbContext.CosechasCafe.AnyAsync(x => x.CultivoId == cultivoId, cancellationToken))
        {
            blockedBy.Add("cosechas");
        }

        if (await dbContext.Costos.AnyAsync(x => x.CultivoId == cultivoId, cancellationToken))
        {
            blockedBy.Add("costos");
        }

        return blockedBy;
    }

    private static bool IsForeignKeyViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException { Number: 547 };
    }
}
