using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Health;

/// <summary>
/// Comprueba que la API puede abrir conexión con la base de datos configurada.
/// No propaga excepciones ni detalles de conexión al reporte para no exponer
/// información sensible en <c>/health/ready</c>.
/// </summary>
public sealed class DatabaseHealthCheck(ProyectoCafeDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
