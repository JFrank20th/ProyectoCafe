using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken ct);

    Task<Cliente?> FindAsync(int clienteId, CancellationToken ct);

    Task<bool> NumeroIdentificacionExistsAsync(
        string numeroIdentificacion,
        int? excludeClienteId,
        CancellationToken ct);

    Task AddAsync(Cliente cliente, CancellationToken ct);
}