using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Negocio.MetodosPago;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Negocio.MetodosPago;

public sealed class MetodoPagoRepository(ProyectoCafeDbContext dbContext) : IMetodoPagoRepository
{
    public async Task<IReadOnlyList<MetodoPagoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.MetodosPago
            .AsNoTracking()
            .OrderBy(x => x.MetodoPagoId)
            .Select(x => new MetodoPagoResponse
            {
                MetodoPagoId = x.MetodoPagoId,
                Nombre = x.Nombre,
                Estado = x.Estado ?? true,
            })
            .ToListAsync(cancellationToken);
    }
}
