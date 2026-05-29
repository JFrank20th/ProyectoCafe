namespace ProyectoCafe.Application.Negocio.MetodosPago;

public interface IMetodoPagoRepository
{
    Task<IReadOnlyList<MetodoPagoResponse>> GetAllAsync(CancellationToken cancellationToken);
}
