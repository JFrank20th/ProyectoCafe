namespace ProyectoCafe.Application.Inventario.UnidadesMedida;

public interface IUnidadMedidaRepository
{
    Task<IReadOnlyList<UnidadMedidaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<UnidadMedidaResponse?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken);

    Task<string> CreateAsync(UnidadMedidaRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(string codigo, UnidadMedidaRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string codigo, CancellationToken cancellationToken);
}
