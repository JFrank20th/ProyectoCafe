using Microsoft.Extensions.Configuration;
using ProyectoCafe.API.Configuration;

namespace ProyectoCafe.Tests;

public sealed class CorsOriginsTests
{
    [Fact]
    public void Resolve_NormalizesAndDeduplicatesOrigins()
    {
        var configuration = BuildConfiguration(
            ("Cors:AllowedOrigins:0", "http://localhost:3010/"),
            ("Cors:AllowedOrigins:1", "HTTP://LOCALHOST:3010"));

        var origins = CorsOrigins.Resolve(configuration);

        Assert.Equal(new[] { "http://localhost:3010" }, origins);
    }

    [Fact]
    public void Resolve_ReadsCommaSeparatedEnvironmentVariableFallback()
    {
        var configuration = BuildConfiguration(
            (CorsOrigins.EnvironmentVariable, "https://portal.example.com, https://admin.example.com;https://portal.example.com"));

        var origins = CorsOrigins.Resolve(configuration);

        Assert.Equal(
            new[] { "https://portal.example.com", "https://admin.example.com" },
            origins);
    }

    [Fact]
    public void Resolve_RejectsWildcard()
    {
        var configuration = BuildConfiguration(("Cors:AllowedOrigins:0", "*"));

        var exception = Assert.Throws<InvalidOperationException>(() => CorsOrigins.Resolve(configuration));

        Assert.Contains("comodines", exception.Message);
    }

    [Theory]
    [InlineData("https://portal.example.com/app")]
    [InlineData("https://portal.example.com?tenant=1")]
    [InlineData("https://portal.example.com#section")]
    [InlineData("ftp://portal.example.com")]
    [InlineData("portal.example.com")]
    public void Resolve_RejectsInvalidOrigins(string origin)
    {
        var configuration = BuildConfiguration(("Cors:AllowedOrigins:0", origin));

        Assert.Throws<InvalidOperationException>(() => CorsOrigins.Resolve(configuration));
    }

    [Fact]
    public void Resolve_ThrowsWhenNoOriginsAreConfigured()
    {
        var configuration = BuildConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(() => CorsOrigins.Resolve(configuration));

        Assert.Contains("CORS no está configurado", exception.Message);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value =>
                new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();
    }
}
