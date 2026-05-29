using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Produccion.Fincas;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Produccion.Fincas;

public sealed class FincaRepository(ProyectoCafeDbContext dbContext) : IFincaRepository
{
    public async Task<IReadOnlyList<FincaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Fincas
            .AsNoTracking()
            .OrderBy(x => x.FincaId)
            .Select(x => new FincaResponse
            {
                FincaId = x.FincaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Ubicacion = x.Ubicacion,
                Municipio = x.Municipio,
                Departamento = x.Departamento,
                AreaHectareas = x.AreaHectareas,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FincaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Fincas
            .AsNoTracking()
            .Where(x => x.FincaId == id)
            .Select(x => new FincaResponse
            {
                FincaId = x.FincaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Ubicacion = x.Ubicacion,
                Municipio = x.Municipio,
                Departamento = x.Departamento,
                AreaHectareas = x.AreaHectareas,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(FincaRequest request, CancellationToken cancellationToken)
    {
        var entity = new Finca
        {
            Nombre = request.Nombre ?? string.Empty,
            Descripcion = NullIfWhiteSpace(request.Descripcion),
            Ubicacion = NullIfWhiteSpace(request.Ubicacion),
            Municipio = NullIfWhiteSpace(request.Municipio),
            Departamento = NullIfWhiteSpace(request.Departamento),
            AreaHectareas = request.AreaHectareas,
            Estado = request.Estado,
        };

        dbContext.Fincas.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.FincaId;
    }

    public async Task<bool> UpdateAsync(int id, FincaRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Fincas
            .SingleOrDefaultAsync(x => x.FincaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Nombre = request.Nombre ?? entity.Nombre;
        entity.Descripcion = NullIfWhiteSpace(request.Descripcion);
        entity.Ubicacion = NullIfWhiteSpace(request.Ubicacion);
        entity.Municipio = NullIfWhiteSpace(request.Municipio);
        entity.Departamento = NullIfWhiteSpace(request.Departamento);
        entity.AreaHectareas = request.AreaHectareas;
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Fincas
            .SingleOrDefaultAsync(x => x.FincaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Fincas.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
