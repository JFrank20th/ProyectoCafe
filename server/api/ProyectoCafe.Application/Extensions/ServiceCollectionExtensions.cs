using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProyectoCafe.Application.Services;

namespace ProyectoCafe.Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los casos de uso de la capa Application. Las implementaciones de
    /// persistencia y de infraestructura se registran desde Infrastructure/API.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationService>();
        services.AddScoped<AdministrationService>();

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
