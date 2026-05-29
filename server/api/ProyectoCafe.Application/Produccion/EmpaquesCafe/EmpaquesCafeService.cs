namespace ProyectoCafe.Application.Produccion.EmpaquesCafe;

public sealed class EmpaquesCafeService(IEmpaqueCafeRepository repository) : IEmpaquesCafeService
{
    public Task<IReadOnlyList<EmpaqueCafeResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<EmpaqueCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<EmpaqueCafeCreateResult> CreateAsync(EmpaqueCafeCreateRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> CancelAsync(int id, CancellationToken cancellationToken) =>
        repository.CancelAsync(id, cancellationToken);
}
