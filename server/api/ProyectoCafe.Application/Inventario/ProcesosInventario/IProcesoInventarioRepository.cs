namespace ProyectoCafe.Application.Inventario.ProcesosInventario;

public interface IProcesoInventarioRepository
{
    Task<IReadOnlyList<ProcesoInventarioResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProcesoInventarioResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProcesoInventarioCreateResult> CreateAsync(ProcesoInventarioCreateRequest request, CancellationToken cancellationToken);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
}