namespace ProyectoCafe.Application.Inventario.Lotes;

public interface ILotesService
{
    Task<IReadOnlyList<LoteExistenciaResumenResponse>> GetSummaryAsync(int? productoId, bool soloActivos, bool soloConStock, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoteResponse>> GetAllAsync(int? productoId, bool soloActivos, bool soloConStock, CancellationToken cancellationToken);

    Task<LoteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<LoteTraceabilityResponse?> GetTraceabilityAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(LoteRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, LoteUpdateRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
