namespace ProyectoCafe.Application.Negocio.TiposVenta;

public interface ITipoVentaRepository
{
    Task<IReadOnlyList<TipoVentaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<TipoVentaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(TipoVentaRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, TipoVentaRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
