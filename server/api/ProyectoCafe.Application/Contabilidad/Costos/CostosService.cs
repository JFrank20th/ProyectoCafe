namespace ProyectoCafe.Application.Contabilidad.Costos;

public sealed class CostosService(ICostoRepository repository) : ICostosService
{
    public Task<IReadOnlyList<CostoResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<CostoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(CostoRequest request, CancellationToken cancellationToken)
        => repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, CostoRequest request, CancellationToken cancellationToken)
        => repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => repository.DeleteAsync(id, cancellationToken);
}
