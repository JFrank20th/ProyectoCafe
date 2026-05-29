namespace ProyectoCafe.Application.Produccion.Fincas;

public interface IFincasService
{
    Task<IReadOnlyList<FincaResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<FincaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(FincaRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, FincaRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
