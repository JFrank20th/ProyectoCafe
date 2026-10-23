using Microsoft.Extensions.DependencyInjection;

namespace ProyectoCafe.Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios de aplicación. El alcance actual del sistema se limita a
    /// Administración y Dashboard, cuyos endpoints consumen directamente el contexto
    /// de datos, por lo que no hay servicios de negocio adicionales que registrar.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
