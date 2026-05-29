namespace ProyectoCafe.Application.Inventario.Categorias;

public sealed class CategoriasService(ICategoriaRepository repository) : ICategoriasService
{
    public Task<IReadOnlyList<CategoriaResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<CategoriaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(CategoriaRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, CategoriaRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
