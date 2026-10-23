using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class AuthLoginEndpointTests
{
    private const string Password = ProyectoCafeApiFactory.DefaultPassword;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndSetsAuthCookies()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("valid@test.local", password: Password);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "valid@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var joinedCookies = string.Join(';', cookies!);
        Assert.Contains("pc_at=", joinedCookies);
        Assert.Contains("pc_csrf=", joinedCookies);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("valid@test.local", body.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccountForFifteenMinutes()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var userId = await factory.SeedUserAsync("locked@test.local", password: Password);
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = "locked@test.local",
                password = "wrong-password",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);

            using var failedBody = JsonDocument.Parse(await failed.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_CREDENTIALS", failedBody.RootElement.GetProperty("error").GetProperty("code").GetString());
        }

        // El sexto intento, incluso con la contraseña correcta, debe respetar el bloqueo persistente.
        var locked = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "locked@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.NotNull(locked.Headers.RetryAfter);
        Assert.True(locked.Headers.RetryAfter!.Delta > TimeSpan.Zero);

        using var lockedBody = JsonDocument.Parse(await locked.Content.ReadAsStringAsync());
        var error = lockedBody.RootElement.GetProperty("error");
        Assert.Equal("TOO_MANY_REQUESTS", error.GetProperty("code").GetString());
        Assert.Equal("Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.", error.GetProperty("message").GetString());

        var user = await factory.GetUserAsync(userId);
        Assert.Equal(5, user.IntentosFallidosLogin);
        Assert.NotNull(user.LockoutFin);
        Assert.InRange(user.LockoutFin!.Value, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task Login_RateLimitPerIp_BlocksAfterConfiguredFailures()
    {
        await using var factory = new ProyectoCafeApiFactory(new Dictionary<string, string?>
        {
            ["Auth:RateLimit:Enabled"] = "true",
            ["Auth:RateLimit:IpMaxFailedAttempts"] = "2",
            ["Auth:RateLimit:IpWindowMinutes"] = "5",
            ["Auth:RateLimit:UserMaxFailedAttempts"] = "100",
            ["Auth:RateLimit:UserWindowMinutes"] = "15",
        });
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = $"unknown-{attempt}@test.local",
                password = "wrong-password",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "unknown-3@test.local",
            password = "wrong-password",
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        Assert.True(blocked.Headers.RetryAfter!.Delta > TimeSpan.Zero);

        using var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("TOO_MANY_REQUESTS", error.GetProperty("code").GetString());
        Assert.Equal("Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_FailedAttempt_LogsSuspiciousAttemptWithoutPassword()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("victim@test.local", password: Password);
        using var client = factory.CreateClient();

        var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "victim@test.local",
            password = "wrong-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);

        var warning = factory.Logs.Entries.FirstOrDefault(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Category == "ProyectoCafe.API.Auth.Login" &&
            entry.Message.Contains("Login fallido", StringComparison.Ordinal));

        Assert.NotNull(warning);
        Assert.Contains("v***@test.local", warning!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong-password", warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, warning.Message, StringComparison.Ordinal);
    }
}
