namespace ProyectoCafe.Application.Compras.Proveedores;

public interface IProveedoresService
{
    Task<IReadOnlyList<ProveedorResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProveedorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(ProveedorRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, ProveedorRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
