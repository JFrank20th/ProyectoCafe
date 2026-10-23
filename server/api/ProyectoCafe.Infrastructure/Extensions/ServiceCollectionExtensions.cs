using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.Infrastructure.Configuration;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
        if (string.IsNullOrWhiteSpace(dbOptions?.ConnectionString))
        {
            throw new InvalidOperationException($"Missing required config: {DatabaseOptions.SectionName}:ConnectionString");
        }

        services.AddDbContext<ProyectoCafeDbContext>(options =>
            options.UseSqlServer(dbOptions.ConnectionString));

        return services;
    }
}
