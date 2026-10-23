using ProyectoCafe.API.Auth;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class SecurityConfigurationTests
{
    private const string DevAdminHash =
        "pbkdf2_sha256$600000$EacqfrHKC6g+eZkqMK0rLQ==$qMkQMLyn7ga7QUSkyRRUZm5FxQpcNj3CIbjrBbDBLnA=";

    [Fact]
    public void Production_WithoutSigningKey_FailsAtStartup()
    {
        using var factory = new ProyectoCafeApiFactory(
            new Dictionary<string, string?>
            {
                ["Auth:Jwt:SigningKey"] = string.Empty,
                ["Cors:AllowedOrigins:0"] = "https://portal.example.com",
            },
            environment: "Production");

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Auth:Jwt:SigningKey", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("requerido", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_WithProvisionalSigningKey_FailsAtStartup()
    {
        using var factory = new ProyectoCafeApiFactory(
            new Dictionary<string, string?>
            {
                ["Auth:Jwt:SigningKey"] = "CHANGE_ME_DEV_ONLY_please_use_env_var",
                ["Cors:AllowedOrigins:0"] = "https://portal.example.com",
            },
            environment: "Production");

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Auth:Jwt:SigningKey", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("provisional", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_WithSeededUsers_FailsAtStartup()
    {
        using var factory = new ProyectoCafeApiFactory(
            new Dictionary<string, string?>
            {
                ["Auth:Users:0:Email"] = "admin@local",
                ["Auth:Users:0:Name"] = "Admin",
                ["Auth:Users:0:PasswordHash"] = DevAdminHash,
                ["Auth:Users:0:Roles:0"] = "Admin",
                ["Cors:AllowedOrigins:0"] = "https://portal.example.com",
            },
            environment: "Production");

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Auth:Users", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Development", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_WithPlaintextPassword_FailsAtStartup()
    {
        using var factory = new ProyectoCafeApiFactory(
            new Dictionary<string, string?>
            {
                ["Auth:Users:0:Password"] = "admin",
            },
            environment: "Development");

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("texto plano", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KnownDemoAccountsWithDefaultPasswords_AreDetected()
    {
        var adminHash = PasswordHasher.HashPassword("admin", iterations: 100_000);
        var userHash = PasswordHasher.HashPassword("user", iterations: 100_000);
        var strongHash = PasswordHasher.HashPassword("A-strong-password!", iterations: 100_000);

        Assert.True(SecurityDefaults.IsKnownDefaultCredential("admin@local", adminHash));
        Assert.True(SecurityDefaults.IsKnownDefaultCredential("ADMIN@LOCAL", "admin"));
        Assert.True(SecurityDefaults.IsKnownDefaultCredential("user@local", userHash));
        Assert.False(SecurityDefaults.IsKnownDefaultCredential("admin@local", strongHash));
        Assert.False(SecurityDefaults.IsKnownDefaultCredential("someone@example.com", adminHash));
    }

    [Fact]
    public void PasswordHashes_UseStrongIterationCountAndSamePasswordCanBeVerified()
    {
        var hash = PasswordHasher.HashPassword("A-strong-password!");

        Assert.True(PasswordHasher.TryParse(hash, out var iterations, out var salt, out var digest));
        Assert.Equal(PasswordHasher.DefaultIterations, iterations);
        Assert.Equal(16, salt.Length);
        Assert.Equal(32, digest.Length);
        Assert.True(PasswordHasher.Verify("A-strong-password!", hash));
        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }
}
