namespace ProyectoCafe.Application.Produccion.Cultivos;

public interface ICultivosService
{
    Task<IReadOnlyList<CultivoResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken);

    Task<CultivoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(CultivoRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, CultivoRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
