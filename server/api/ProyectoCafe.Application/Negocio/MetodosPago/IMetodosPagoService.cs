namespace ProyectoCafe.Application.Negocio.MetodosPago;

public interface IMetodosPagoService
{
    Task<IReadOnlyList<MetodoPagoResponse>> GetAllAsync(CancellationToken cancellationToken);
}
