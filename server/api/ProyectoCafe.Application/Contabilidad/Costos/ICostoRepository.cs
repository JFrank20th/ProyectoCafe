namespace ProyectoCafe.Application.Contabilidad.Costos;

public interface ICostoRepository
{
    Task<IReadOnlyList<CostoResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CostoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(CostoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, CostoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
