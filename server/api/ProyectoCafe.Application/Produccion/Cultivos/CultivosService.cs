namespace ProyectoCafe.Application.Produccion.Cultivos;

public sealed class CultivosService(ICultivoRepository repository) : ICultivosService
{
    public Task<IReadOnlyList<CultivoResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken) =>
        repository.GetAllAsync(fincaId, cancellationToken);

    public Task<CultivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(CultivoRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, CultivoRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
