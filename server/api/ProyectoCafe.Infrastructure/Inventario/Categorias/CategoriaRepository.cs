using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.Categorias;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.Categorias;

public sealed class CategoriaRepository(ProyectoCafeDbContext dbContext) : ICategoriaRepository
{
    public async Task<IReadOnlyList<CategoriaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categorias
            .AsNoTracking()
            .OrderBy(x => x.CategoriaId)
            .Select(x => new CategoriaResponse
            {
                Id = x.CategoriaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoriaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Categorias
            .AsNoTracking()
            .Where(x => x.CategoriaId == id)
            .Select(x => new CategoriaResponse
            {
                Id = x.CategoriaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CategoriaRequest request, CancellationToken cancellationToken)
    {
        var entity = new Categoria
        {
            Nombre = request.Nombre ?? string.Empty,
            Descripcion = request.Descripcion,
            Estado = request.Estado,
        };

        dbContext.Categorias.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.CategoriaId;
    }

    public async Task<bool> UpdateAsync(int id, CategoriaRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Categorias
            .SingleOrDefaultAsync(x => x.CategoriaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Nombre = request.Nombre ?? entity.Nombre;
        entity.Descripcion = request.Descripcion;
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Categorias
            .SingleOrDefaultAsync(x => x.CategoriaId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Categorias.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
