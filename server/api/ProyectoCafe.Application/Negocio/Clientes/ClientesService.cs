namespace ProyectoCafe.Application.Negocio.Clientes;

public sealed class ClientesService(IClienteRepository repository) : IClientesService
{
    public Task<IReadOnlyList<ClienteResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<ClienteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(ClienteRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, ClienteRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
