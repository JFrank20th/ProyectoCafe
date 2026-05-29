namespace ProyectoCafe.Application.Inventario.Insumos;

public interface IInsumoRepository
{
    Task<IReadOnlyList<InsumoResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<InsumoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(InsumoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, InsumoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
