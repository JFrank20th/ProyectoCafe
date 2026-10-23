namespace ProyectoCafe.Application.Inventario.ProcesosInventario;

public sealed class ProcesosInventarioService(IProcesoInventarioRepository repository) : IProcesosInventarioService
{
    public Task<IReadOnlyList<ProcesoInventarioResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<ProcesoInventarioResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => repository.GetByIdAsync(id, cancellationToken);

    public Task<ProcesoInventarioCreateResult> CreateAsync(ProcesoInventarioCreateRequest request, CancellationToken cancellationToken)
        => repository.CreateAsync(request, cancellationToken);

    public Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
        => repository.CancelAsync(id, cancellationToken);
}