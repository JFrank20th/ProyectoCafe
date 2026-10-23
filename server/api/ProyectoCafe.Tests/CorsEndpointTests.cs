using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class CorsEndpointTests
{
    private const string AllowedOrigin = "http://localhost:3010";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_ReturnsExplicitOriginAndCredentials()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        using var request = BuildPreflight(AllowedOrigin);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_DoesNotReturnCorsPermissions()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        using var request = BuildPreflight("https://evil.example");
        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task SimpleRequest_FromAllowedOrigin_NeverUsesWildcardWithCredentials()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("Origin", AllowedOrigin);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var allowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").Single();
        Assert.Equal(AllowedOrigin, allowOrigin);
        Assert.NotEqual("*", allowOrigin);
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task LoginAndSession_FromAllowedOrigin_KeepWorkingWithCookies()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("cors@test.local", password: ProyectoCafeApiFactory.DefaultPassword);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "cors@test.local",
                password = ProyectoCafeApiFactory.DefaultPassword,
            }),
        };
        loginRequest.Headers.TryAddWithoutValidation("Origin", AllowedOrigin);

        using var loginResponse = await client.SendAsync(loginRequest);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(AllowedOrigin, loginResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", loginResponse.Headers.GetValues("Access-Control-Allow-Credentials").Single());

        var csrfToken = ProyectoCafeApiFactory.ReadCookie(loginResponse, "pc_csrf");
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.TryAddWithoutValidation("Origin", AllowedOrigin);
        using var meResponse = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Equal(AllowedOrigin, meResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Development_UsesTheLocalPortalOriginFromAppSettings()
    {
        // Sin el origen inyectado por la factory: debe provenir de
        // appsettings.Development.json.
        await using var factory = new ProyectoCafeApiFactory(
            environment: "Development",
            configureDefaultCorsOrigin: false);
        using var client = factory.CreateClient();

        using var request = BuildPreflight(AllowedOrigin);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task Production_WithConfiguredOrigin_AllowsOnlyThatOrigin()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://portal.example.com",
        };

        await using var factory = new ProyectoCafeApiFactory(overrides, environment: "Production");
        using var client = factory.CreateClient();

        using var allowedRequest = BuildPreflight("https://portal.example.com");
        using var allowedResponse = await client.SendAsync(allowedRequest);

        Assert.Equal(HttpStatusCode.NoContent, allowedResponse.StatusCode);
        Assert.Equal(
            "https://portal.example.com",
            allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var deniedRequest = BuildPreflight(AllowedOrigin);
        using var deniedResponse = await client.SendAsync(deniedRequest);

        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void Production_WithoutAllowedOrigins_FailsAtStartup()
    {
        using var factory = new ProyectoCafeApiFactory(
            environment: "Production",
            configureDefaultCorsOrigin: false);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("CORS", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static HttpRequestMessage BuildPreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.TryAddWithoutValidation("Origin", origin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type,x-csrf");
        return request;
    }
}
