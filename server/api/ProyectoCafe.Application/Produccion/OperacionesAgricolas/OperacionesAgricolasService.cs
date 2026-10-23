namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public sealed class OperacionesAgricolasService(IOperacionAgricolaRepository repository) : IOperacionesAgricolasService
{
    public Task<IReadOnlyList<OperacionAgricolaResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<OperacionAgricolaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<OperacionAgricolaCreateResult> CreateAsync(OperacionAgricolaCreateRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, OperacionAgricolaCreateRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> CancelAsync(int id, CancellationToken cancellationToken) =>
        repository.CancelAsync(id, cancellationToken);
}
