namespace ProyectoCafe.Application.Compras.RecepcionesCafe;

public interface IRecepcionesCafeService
{
    Task<IReadOnlyList<RecepcionCafeResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<RecepcionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RecepcionCafeCreateResult> CreateAsync(RecepcionCafeCreateRequest request, CancellationToken cancellationToken);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
}
