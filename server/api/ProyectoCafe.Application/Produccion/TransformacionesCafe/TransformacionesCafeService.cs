namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public sealed class TransformacionesCafeService(ITransformacionCafeRepository repository) : ITransformacionesCafeService
{
    public Task<IReadOnlyList<TransformacionCafeResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<TransformacionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<TransformacionCafeCreateResult> CreateAsync(TransformacionCafeCreateRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> CancelAsync(int id, CancellationToken cancellationToken) =>
        repository.CancelAsync(id, cancellationToken);
}
