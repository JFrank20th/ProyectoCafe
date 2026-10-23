using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.API.Auth;

public sealed class AuthOptions
{
    public JwtOptions Jwt { get; init; } = new();

    public AuthCookieOptions Cookie { get; init; } = new();

    public LoginRateLimitOptions RateLimit { get; init; } = new();

    public List<DemoUserOptions> Users { get; init; } = [];
}

public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; init; } = "ProyectoCafe";

    [Required]
    public string Audience { get; init; } = "ProyectoCafe.Portal";

    [Required]
    public string SigningKey { get; init; } = string.Empty;

    [Range(5, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 8 * 60;
}

public sealed class DemoUserOptions
{
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Name { get; init; } = string.Empty;

    // Dev/demo compatibility: allow either plaintext Password or PasswordHash.
    // Validation is performed in AuthOptionsValidator.
    public string? Password { get; init; }

    public string? PasswordHash { get; init; }

    public List<string> Roles { get; init; } = [];
}

public sealed class AuthCookieOptions
{
    [Required]
    public string AccessTokenCookieName { get; init; } = "pc_at";

    [Required]
    public string CsrfCookieName { get; init; } = "pc_csrf";

    [Required]
    public string CsrfHeaderName { get; init; } = "X-CSRF";
}
