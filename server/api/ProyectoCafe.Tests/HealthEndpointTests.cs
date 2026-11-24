using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Liveness_IsAnonymousAndDoesNotDependOnDatabase()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_WithHealthyDatabase_IsAnonymousAndDoesNotExposeDetails()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InMemory", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_WithoutReachableDatabase_ReturnsServiceUnavailableWithoutDetails()
    {
        await using var factory = new ProyectoCafeApiFactory(
            configureServices: services =>
            {
                services.RemoveAll<DbContextOptions<ProyectoCafeDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ProyectoCafeDbContext>>();
                services.RemoveAll<ProyectoCafeDbContext>();
                services.AddDbContext<ProyectoCafeDbContext>(options =>
                    options.UseSqlServer(
                        "Server=127.0.0.1,1;Database=readiness;User Id=probe;Password=probe;Connect Timeout=1;TrustServerCertificate=True"));
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Unhealthy", body, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
    }
}
