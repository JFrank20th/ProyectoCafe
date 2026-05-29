namespace ProyectoCafe.Application.Produccion.EmpaquesCafe;

public interface IEmpaqueCafeRepository
{
    Task<IReadOnlyList<EmpaqueCafeResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<EmpaqueCafeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<EmpaqueCafeCreateResult> CreateAsync(EmpaqueCafeCreateRequest request, CancellationToken cancellationToken);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
}
