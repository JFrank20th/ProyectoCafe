namespace ProyectoCafe.Application.Negocio.Clientes;

public interface IClienteRepository
{
    Task<IReadOnlyList<ClienteResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClienteResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(ClienteRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, ClienteRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
