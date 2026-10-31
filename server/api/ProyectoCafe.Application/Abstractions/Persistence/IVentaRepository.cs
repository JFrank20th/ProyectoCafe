using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IVentaRepository
{
    Task<IReadOnlyList<Venta>> ListAsync(CancellationToken ct);

    Task<Venta?> FindAsync(int ventaId, CancellationToken ct);

    Task<int> GetNextNumeroAsync(CancellationToken ct);

    Task AddAsync(Venta venta, CancellationToken ct);
}