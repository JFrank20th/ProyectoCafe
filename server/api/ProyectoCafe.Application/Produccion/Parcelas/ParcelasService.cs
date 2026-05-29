namespace ProyectoCafe.Application.Produccion.Parcelas;

public sealed class ParcelasService(IParcelaRepository repository) : IParcelasService
{
    public Task<IReadOnlyList<ParcelaResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken) =>
        repository.GetAllAsync(fincaId, cancellationToken);

    public Task<ParcelaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(ParcelaRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, ParcelaRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
