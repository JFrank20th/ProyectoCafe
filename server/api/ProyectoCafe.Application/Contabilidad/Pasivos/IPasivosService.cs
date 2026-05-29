namespace ProyectoCafe.Application.Contabilidad.Pasivos;

public interface IPasivosService
{
    Task<IReadOnlyList<PasivoResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PasivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(PasivoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, PasivoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
