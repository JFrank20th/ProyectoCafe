namespace ProyectoCafe.Application.Negocio.MetodosPago;

public sealed class MetodosPagoService(IMetodoPagoRepository repository) : IMetodosPagoService
{
    public Task<IReadOnlyList<MetodoPagoResponse>> GetAllAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
