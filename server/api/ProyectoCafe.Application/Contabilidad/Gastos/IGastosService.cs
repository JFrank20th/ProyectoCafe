namespace ProyectoCafe.Application.Contabilidad.Gastos;

public interface IGastosService
{
    Task<IReadOnlyList<GastoResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<GastoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(GastoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, GastoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
