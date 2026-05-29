namespace ProyectoCafe.Application.Inventario.Presentaciones;

public interface IPresentacionesService
{
    Task<IReadOnlyList<PresentacionResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PresentacionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(PresentacionRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, PresentacionRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}