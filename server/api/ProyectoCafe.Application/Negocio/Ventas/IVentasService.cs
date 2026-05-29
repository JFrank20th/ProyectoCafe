namespace ProyectoCafe.Application.Negocio.Ventas;

public interface IVentasService
{
    Task<IReadOnlyList<VentaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<VentaViewResponse?> GetViewAsync(int ventaId, CancellationToken cancellationToken);

    Task<int> CreateAsync(VentaCreateRequest request, CancellationToken cancellationToken);

    Task<bool> AnnulAsync(int ventaId, CancellationToken cancellationToken);
}
