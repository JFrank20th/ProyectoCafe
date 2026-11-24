using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.API.Auth;

public sealed class AuthOptions
{
    public JwtOptions Jwt { get; init; } = new();

    public AuthCookieOptions Cookie { get; init; } = new();

    public LoginRateLimitOptions RateLimit { get; init; } = new();

    /// <summary>
    /// Usuarios iniciales. Solo se permiten en Development: en el resto de
    /// entornos la aplicación falla al arrancar si esta lista no está vacía.
    /// </summary>
    public List<BootstrapUserOptions> Users { get; init; } = [];
}

public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; init; } = "ProyectoCafe";

    [Required]
    public string Audience { get; init; } = "ProyectoCafe.Portal";

    /// <summary>
    /// Clave de firma HS256. No tiene valor por defecto: debe configurarse
    /// mediante variable de entorno (Auth__Jwt__SigningKey), user-secrets en
    /// Development o un gestor de secretos. Debe tener al menos 32 bytes.
    /// </summary>
    [Required]
    public string SigningKey { get; init; } = string.Empty;

    [Range(5, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 8 * 60;
}

public sealed class BootstrapUserOptions
{
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Hash PBKDF2 de la contraseña. Nunca se acepta una contraseña en texto
    /// plano en la configuración.
    /// </summary>
    [Required]
    public string PasswordHash { get; init; } = string.Empty;

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

    /// <summary>
    /// Política SameSite aplicada a <c>pc_at</c> y <c>pc_csrf</c>: Lax (por
    /// defecto), Strict o None. None solo es necesario cuando el Portal y la
    /// API están en sitios distintos y exige Secure.
    /// </summary>
    [Required]
    public string SameSite { get; init; } = "Lax";

    /// <summary>
    /// Fuerza el atributo Secure de las cookies. Si no se define, es
    /// <c>true</c> fuera de Development y <c>false</c> en Development.
    /// </summary>
    public bool? Secure { get; init; }

    public bool ResolveSecure(IHostEnvironment environment) =>
        Secure ?? !environment.IsDevelopment();

    public bool TryGetSameSite(out SameSiteMode mode)
    {
        switch ((SameSite ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "lax":
                mode = SameSiteMode.Lax;
                return true;
            case "strict":
                mode = SameSiteMode.Strict;
                return true;
            case "none":
                mode = SameSiteMode.None;
                return true;
            default:
                mode = SameSiteMode.Lax;
                return false;
        }
    }
}
