using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Inventario.UnidadesMedida;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Inventario.UnidadesMedida;

public sealed class UnidadMedidaRepository(ProyectoCafeDbContext dbContext) : IUnidadMedidaRepository
{
    public async Task<IReadOnlyList<UnidadMedidaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.UnidadesMedida
            .AsNoTracking()
            .OrderBy(x => x.Codigo)
            .Select(x => new UnidadMedidaResponse
            {
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UnidadMedidaResponse?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        var normalizedCodigo = NormalizeCodigo(codigo);

        return await dbContext.UnidadesMedida
            .AsNoTracking()
            .Where(x => x.Codigo == normalizedCodigo)
            .Select(x => new UnidadMedidaResponse
            {
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Estado = x.Estado ?? true,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(UnidadMedidaRequest request, CancellationToken cancellationToken)
    {
        var codigo = NormalizeCodigo(request.Codigo);
        var nombre = NormalizeNombre(request.Nombre)
            ?? throw new ArgumentException("Nombre es requerido.", nameof(request));

        var entity = new UnidadMedida
        {
            Codigo = codigo,
            Nombre = nombre,
            Estado = request.Estado,
        };

        dbContext.UnidadesMedida.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Codigo;
    }

    public async Task<bool> UpdateAsync(string codigo, UnidadMedidaRequest request, CancellationToken cancellationToken)
    {
        var normalizedCodigo = NormalizeCodigo(codigo);

        var entity = await dbContext.UnidadesMedida
            .SingleOrDefaultAsync(x => x.Codigo == normalizedCodigo, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var normalizedNombre = NormalizeNombre(request.Nombre);
        if (normalizedNombre is not null)
        {
            entity.Nombre = normalizedNombre;
        }
        entity.Estado = request.Estado;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string codigo, CancellationToken cancellationToken)
    {
        var normalizedCodigo = NormalizeCodigo(codigo);

        var entity = await dbContext.UnidadesMedida
            .SingleOrDefaultAsync(x => x.Codigo == normalizedCodigo, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.UnidadesMedida.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeCodigo(string? codigo)
    {
        return (codigo ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string? NormalizeNombre(string? nombre)
    {
        var value = (nombre ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
