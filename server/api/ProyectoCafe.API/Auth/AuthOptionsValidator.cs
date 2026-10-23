using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

namespace ProyectoCafe.API.Auth;

public sealed class AuthOptionsValidator(
    IHostEnvironment environment,
    IConfiguration configuration) : IValidateOptions<AuthOptions>
{
    private readonly IHostEnvironment _environment = environment;
    private readonly IConfiguration _configuration = configuration;

    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        ValidateSigningKey(options.Jwt.SigningKey, failures);
        ValidateUsers(options, failures);

        if (options.Jwt.AccessTokenLifetimeMinutes is < 5 or > 24 * 60)
        {
            failures.Add("Auth:Jwt:AccessTokenLifetimeMinutes debe estar entre 5 y 1440.");
        }

        if (string.IsNullOrWhiteSpace(options.Cookie.AccessTokenCookieName))
            failures.Add("Auth:Cookie:AccessTokenCookieName es requerido.");
        if (string.IsNullOrWhiteSpace(options.Cookie.CsrfCookieName))
            failures.Add("Auth:Cookie:CsrfCookieName es requerido.");
        if (string.IsNullOrWhiteSpace(options.Cookie.CsrfHeaderName))
            failures.Add("Auth:Cookie:CsrfHeaderName es requerido.");

        var rateLimit = options.RateLimit;
        if (rateLimit.IpMaxFailedAttempts is < 1 or > 10_000)
            failures.Add("Auth:RateLimit:IpMaxFailedAttempts debe estar entre 1 y 10000.");
        if (rateLimit.IpWindowMinutes is < 1 or > 24 * 60)
            failures.Add("Auth:RateLimit:IpWindowMinutes debe estar entre 1 y 1440.");
        if (rateLimit.UserMaxFailedAttempts is < 1 or > 10_000)
            failures.Add("Auth:RateLimit:UserMaxFailedAttempts debe estar entre 1 y 10000.");
        if (rateLimit.UserWindowMinutes is < 1 or > 24 * 60)
            failures.Add("Auth:RateLimit:UserWindowMinutes debe estar entre 1 y 1440.");
        if (rateLimit.MaxTrackedKeys is < 100 or > 1_000_000)
            failures.Add("Auth:RateLimit:MaxTrackedKeys debe estar entre 100 y 1000000.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateSigningKey(string? signingKey, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            failures.Add(
                $"Auth:Jwt:SigningKey es requerido. Configúralo mediante {SecurityDefaults.SigningKeyEnvironmentVariable} " +
                "o un gestor de secretos; no hay valor por defecto.");
            return;
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < SecurityDefaults.MinimumSigningKeyBytes)
        {
            failures.Add(
                $"Auth:Jwt:SigningKey debe tener al menos {SecurityDefaults.MinimumSigningKeyBytes} bytes " +
                "(256 bits) para HS256.");
        }

        if (SecurityDefaults.IsPlaceholderSigningKey(signingKey))
        {
            failures.Add(
                "Auth:Jwt:SigningKey contiene un valor provisional o inseguro. " +
                $"Genera una clave aleatoria y configúrala mediante {SecurityDefaults.SigningKeyEnvironmentVariable} " +
                "o un gestor de secretos.");
        }
    }

    private void ValidateUsers(AuthOptions options, List<string> failures)
    {
        // La sección se inspecciona directamente para detectar contraseñas en
        // texto plano incluso si el binder ignora propiedades desconocidas.
        foreach (var userSection in _configuration.GetSection("Auth:Users").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(userSection["Password"]))
            {
                failures.Add(
                    $"Auth:Users[{userSection.Key}]: 'Password' en texto plano no está permitido. " +
                    "Configura 'PasswordHash' con un hash PBKDF2.");
            }
        }

        if (options.Users.Count == 0)
        {
            return;
        }

        if (!_environment.IsDevelopment())
        {
            failures.Add(
                $"Auth:Users no está permitido fuera de Development (entorno actual: {_environment.EnvironmentName}). " +
                "Los usuarios de prueba solo deben existir en Development; aprovisiona el administrador inicial por un canal seguro.");
            return;
        }

        var duplicateEmails = options.Users
            .Select(user => (user.Email ?? string.Empty).Trim())
            .Where(email => email.Length > 0)
            .GroupBy(email => email, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var email in duplicateEmails)
        {
            failures.Add($"Auth:Users: el email '{email}' está repetido.");
        }

        foreach (var user in options.Users)
        {
            var label = string.IsNullOrWhiteSpace(user.Email) ? "sin email" : user.Email;

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                failures.Add("Auth:Users: Email es requerido.");
            }
            else if (!user.Email.Contains('@', StringComparison.Ordinal))
            {
                failures.Add($"Auth:Users[{label}]: Email no es válido.");
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                failures.Add($"Auth:Users[{label}]: Name es requerido.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                failures.Add($"Auth:Users[{label}]: PasswordHash es requerido. No se aceptan contraseñas en texto plano.");
                continue;
            }

            if (!PasswordHasher.TryParse(user.PasswordHash, out var iterations, out _, out _))
            {
                failures.Add(
                    $"Auth:Users[{label}]: PasswordHash no tiene formato válido " +
                    "(pbkdf2_sha256$iterations$salt$hash).");
                continue;
            }

            if (iterations < PasswordHasher.MinimumSeedHashIterations)
            {
                failures.Add(
                    $"Auth:Users[{label}]: PasswordHash debe usar al menos " +
                    $"{PasswordHasher.MinimumSeedHashIterations} iteraciones.");
            }
        }
    }
}
