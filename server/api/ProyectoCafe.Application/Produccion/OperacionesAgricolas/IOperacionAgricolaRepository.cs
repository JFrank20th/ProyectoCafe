namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public interface IOperacionAgricolaRepository
{
    Task<IReadOnlyList<OperacionAgricolaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<OperacionAgricolaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<OperacionAgricolaCreateResult> CreateAsync(OperacionAgricolaCreateRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, OperacionAgricolaCreateRequest request, CancellationToken cancellationToken);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
}
