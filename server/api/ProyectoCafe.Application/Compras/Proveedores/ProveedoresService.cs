namespace ProyectoCafe.Application.Compras.Proveedores;

public sealed class ProveedoresService(IProveedorRepository repository) : IProveedoresService
{
    public Task<IReadOnlyList<ProveedorResponse>> GetAllAsync(bool soloExternos, CancellationToken cancellationToken) =>
        repository.GetAllAsync(soloExternos, cancellationToken);

    public Task<ProveedorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(ProveedorRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, ProveedorRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
