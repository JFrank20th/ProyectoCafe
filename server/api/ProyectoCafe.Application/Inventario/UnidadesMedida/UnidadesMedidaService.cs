namespace ProyectoCafe.Application.Inventario.UnidadesMedida;

public sealed class UnidadesMedidaService(IUnidadMedidaRepository repository) : IUnidadesMedidaService
{
    public Task<IReadOnlyList<UnidadMedidaResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<UnidadMedidaResponse?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken) =>
        repository.GetByCodigoAsync(codigo, cancellationToken);

    public Task<string> CreateAsync(UnidadMedidaRequest request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(string codigo, UnidadMedidaRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(codigo, request, cancellationToken);

    public Task<bool> DeleteAsync(string codigo, CancellationToken cancellationToken) =>
        repository.DeleteAsync(codigo, cancellationToken);
}
