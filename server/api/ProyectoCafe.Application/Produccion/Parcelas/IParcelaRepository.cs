namespace ProyectoCafe.Application.Produccion.Parcelas;

public interface IParcelaRepository
{
    Task<IReadOnlyList<ParcelaResponse>> GetAllAsync(int? fincaId, CancellationToken cancellationToken);

    Task<ParcelaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(ParcelaRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, ParcelaRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
