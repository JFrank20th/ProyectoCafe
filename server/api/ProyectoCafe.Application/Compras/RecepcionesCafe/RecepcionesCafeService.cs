namespace ProyectoCafe.Application.Compras.RecepcionesCafe;

public sealed class RecepcionesCafeService(IRecepcionCafeRepository repository) : IRecepcionesCafeService
{
    public Task<IReadOnlyList<RecepcionCafeResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<RecepcionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<RecepcionCafeCreateResult> CreateAsync(RecepcionCafeCreateRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> CancelAsync(int id, CancellationToken cancellationToken) =>
        repository.CancelAsync(id, cancellationToken);
}
