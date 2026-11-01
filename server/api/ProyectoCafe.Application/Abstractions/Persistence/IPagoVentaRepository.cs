using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IPagoVentaRepository
{
    Task AddAsync(PagoVenta pago, CancellationToken ct);

    Task<IReadOnlyList<PagoVenta>> ListByVentaAsync(int ventaId, CancellationToken ct);
}