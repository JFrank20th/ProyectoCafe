namespace ProyectoCafe.Application.Produccion.Fincas;

public sealed class FincasService(IFincaRepository repository) : IFincasService
{
    public Task<IReadOnlyList<FincaResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<FincaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(FincaRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, FincaRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
