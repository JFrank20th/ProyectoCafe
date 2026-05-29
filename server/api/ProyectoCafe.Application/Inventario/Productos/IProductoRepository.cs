namespace ProyectoCafe.Application.Inventario.Productos;

public interface IProductoRepository
{
    Task<IReadOnlyList<ProductoResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProductoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(ProductoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, ProductoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
