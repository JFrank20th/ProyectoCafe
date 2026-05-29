namespace ProyectoCafe.Application.Contabilidad.Pasivos;

public sealed class PasivosService(IPasivoRepository repository) : IPasivosService
{
    public Task<IReadOnlyList<PasivoResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<PasivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => repository.GetByIdAsync(id, cancellationToken);

    public Task<int> CreateAsync(PasivoRequest request, CancellationToken cancellationToken)
        => repository.CreateAsync(request, cancellationToken);

    public Task<bool> UpdateAsync(int id, PasivoRequest request, CancellationToken cancellationToken)
        => repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => repository.DeleteAsync(id, cancellationToken);
}
