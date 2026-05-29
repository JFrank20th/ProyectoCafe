namespace ProyectoCafe.Application.Contabilidad.Gastos;

public sealed class GastosService(IGastoRepository repository) : IGastosService
{
    public Task<IReadOnlyList<GastoResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<GastoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(GastoRequest request, CancellationToken cancellationToken)
        => repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, GastoRequest request, CancellationToken cancellationToken)
        => repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => repository.DeleteAsync(id, cancellationToken);
}
