namespace ProyectoCafe.Application.Inventario.Categorias;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<CategoriaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CategoriaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(CategoriaRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, CategoriaRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
