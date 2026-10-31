using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IPrecioVentaRepository
{
    Task<IReadOnlyList<PrecioVenta>> ListAsync(CancellationToken ct);

    Task<PrecioVenta?> FindAsync(int precioVentaId, CancellationToken ct);

    Task<bool> NombreExistsAsync(
        string nombre,
        int presentacionCafeId,
        int? excludePrecioVentaId,
        CancellationToken ct);

    Task AddAsync(PrecioVenta precioVenta, CancellationToken ct);
}