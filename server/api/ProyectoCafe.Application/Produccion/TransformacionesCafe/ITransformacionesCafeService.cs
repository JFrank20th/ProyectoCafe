namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public interface ITransformacionesCafeService
{
    Task<IReadOnlyList<TransformacionCafeResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<TransformacionCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TransformacionCafeCreateResult> CreateAsync(TransformacionCafeCreateRequest request, CancellationToken cancellationToken);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
}
