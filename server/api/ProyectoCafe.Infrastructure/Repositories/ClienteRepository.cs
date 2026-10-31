using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class ClienteRepository(ProyectoCafeDbContext db) : IClienteRepository
{
    public async Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken ct)
    {
        return await db.Set<Cliente>()
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync(ct);
    }

    public Task<Cliente?> FindAsync(int clienteId, CancellationToken ct)
    {
        return db.Set<Cliente>().FirstOrDefaultAsync(x => x.ClienteId == clienteId, ct);
    }

    public async Task<bool> NumeroIdentificacionExistsAsync(
        string numeroIdentificacion,
        int? excludeClienteId,
        CancellationToken ct)
    {
        return await db.Set<Cliente>().AnyAsync(
            x => x.NumeroIdentificacion == numeroIdentificacion &&
                 (!excludeClienteId.HasValue || x.ClienteId != excludeClienteId.Value),
            ct);
    }

    public Task AddAsync(Cliente cliente, CancellationToken ct)
    {
        db.Set<Cliente>().Add(cliente);
        return Task.CompletedTask;
    }
}