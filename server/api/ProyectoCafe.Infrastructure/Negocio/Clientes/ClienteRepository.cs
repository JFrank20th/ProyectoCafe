using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Negocio.Clientes;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Negocio.Clientes;

public sealed class ClienteRepository(ProyectoCafeDbContext dbContext) : IClienteRepository
{
    public async Task<IReadOnlyList<ClienteResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Clientes
            .AsNoTracking()
            .OrderBy(x => x.ClienteId)
            .Select(x => new ClienteResponse
            {
                ClienteId = x.ClienteId,
                Nombre = x.Nombre,
                Documento = x.Documento,
                Telefono = x.Telefono,
                Email = x.Email,
                Direccion = x.Direccion,
                Estado = x.Estado ?? true,
                FechaCreacion = x.FechaCreacion,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClienteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Clientes
            .AsNoTracking()
            .Where(x => x.ClienteId == id)
            .Select(x => new ClienteResponse
            {
                ClienteId = x.ClienteId,
                Nombre = x.Nombre,
                Documento = x.Documento,
                Telefono = x.Telefono,
                Email = x.Email,
                Direccion = x.Direccion,
                Estado = x.Estado ?? true,
                FechaCreacion = x.FechaCreacion,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(ClienteRequest request, CancellationToken cancellationToken)
    {
        var nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");

        var entity = new Cliente
        {
            Nombre = nombre,
            Documento = NormalizeOptional(request.Documento),
            Telefono = NormalizeOptional(request.Telefono),
            Email = NormalizeOptional(request.Email),
            Direccion = NormalizeOptional(request.Direccion),
            Estado = request.Estado,
        };

        dbContext.Clientes.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ArgumentException("Documento ya existe.");
        }

        return entity.ClienteId;
    }

    public async Task<bool> UpdateAsync(int id, ClienteRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Clientes
            .SingleOrDefaultAsync(x => x.ClienteId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Nombre = NormalizeRequired(request.Nombre, "Nombre es requerido.");
        entity.Documento = NormalizeOptional(request.Documento);
        entity.Telefono = NormalizeOptional(request.Telefono);
        entity.Email = NormalizeOptional(request.Email);
        entity.Direccion = NormalizeOptional(request.Direccion);
        entity.Estado = request.Estado;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ArgumentException("Documento ya existe.");
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Clientes
            .SingleOrDefaultAsync(x => x.ClienteId == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Clientes.Remove(entity);
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
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
