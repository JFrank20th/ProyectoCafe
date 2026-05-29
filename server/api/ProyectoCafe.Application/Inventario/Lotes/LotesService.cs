namespace ProyectoCafe.Application.Inventario.Lotes;

public sealed class LotesService(ILoteRepository repository) : ILotesService
{
    public Task<IReadOnlyList<LoteResponse>> GetAllAsync(int? productoId, bool soloActivos, bool soloConStock, CancellationToken cancellationToken) =>
        repository.GetAllAsync(productoId, soloActivos, soloConStock, cancellationToken);

    public Task<LoteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<LoteTraceabilityResponse?> GetTraceabilityAsync(int id, CancellationToken cancellationToken) =>
        repository.GetTraceabilityAsync(id, cancellationToken);

    public Task<int> CreateAsync(LoteRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, LoteUpdateRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
