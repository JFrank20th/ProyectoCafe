namespace ProyectoCafe.Application.Negocio.Ventas;

public sealed class VentasService(IVentaRepository repository) : IVentasService
{
    public Task<IReadOnlyList<VentaResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<VentaViewResponse?> GetViewAsync(int ventaId, CancellationToken cancellationToken)
        => repository.GetViewAsync(ventaId, cancellationToken);

    public Task<int> CreateAsync(VentaCreateRequest request, CancellationToken cancellationToken)
        => repository.CreateAsync(request, cancellationToken);

    public Task<bool> AnnulAsync(int ventaId, CancellationToken cancellationToken)
        => repository.AnnulAsync(ventaId, cancellationToken);
}
