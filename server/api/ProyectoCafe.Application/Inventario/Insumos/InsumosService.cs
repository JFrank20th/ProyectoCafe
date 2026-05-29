namespace ProyectoCafe.Application.Inventario.Insumos;

public sealed class InsumosService(IInsumoRepository repository) : IInsumosService
{
    public Task<IReadOnlyList<InsumoResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<InsumoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(InsumoRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, InsumoRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
