using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests.Infrastructure;

/// <summary>
/// Factory de integración con SQL Server local.
/// Solo permite la base de datos ProyectoCafe_IntegrationTests.
/// </summary>
public class ProyectoCafeSqlIntegrationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AuthorizedDatabaseName = "ProyectoCafe_IntegrationTests";

    private readonly string _connectionString;

    public ProyectoCafeSqlIntegrationFactory()
    {
        var configured = Environment.GetEnvironmentVariable("PROYECTOCAFE_INTEGRATION_CONNECTION_STRING");
        _connectionString = string.IsNullOrWhiteSpace(configured)
            ? $"Server=localhost;Database={AuthorizedDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;"
            : configured;

        ValidateConnectionString(_connectionString);
    }

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = _connectionString,
                ["Auth:Jwt:Issuer"] = "tests",
                ["Auth:Jwt:Audience"] = "tests",
                ["Auth:Jwt:SigningKey"] = ProyectoCafeApiFactory.DefaultSigningKey,
                ["Auth:Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Auth:Cookie:AccessTokenCookieName"] = "pc_at",
                ["Auth:Cookie:CsrfCookieName"] = "pc_csrf",
                ["Auth:Cookie:CsrfHeaderName"] = "X-CSRF",
                ["Auth:RateLimit:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3010",
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProyectoCafeDbContext>>();
            services.RemoveAll<ProyectoCafeDbContext>();

            services.AddDbContext<ProyectoCafeDbContext>(options =>
                options.UseSqlServer(_connectionString));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    private static void ValidateConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, AuthorizedDatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"La base de datos de integración debe ser '{AuthorizedDatabaseName}'.");
        }
    }
}