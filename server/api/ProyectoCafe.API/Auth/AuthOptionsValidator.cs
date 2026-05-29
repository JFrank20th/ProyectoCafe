using Microsoft.Extensions.Options;
using System.Text;

namespace ProyectoCafe.API.Auth;

public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        var signingKey = options.Jwt.SigningKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            failures.Add("Auth:Jwt:SigningKey es requerido.");
        }
        else if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            failures.Add("Auth:Jwt:SigningKey debe tener al menos 32 bytes (256 bits) para HS256.");
        }
        else if (string.Equals(signingKey, "CHANGE_ME_DEV_ONLY_please_use_env_var", StringComparison.Ordinal))
        {
            failures.Add("Auth:Jwt:SigningKey está en valor por defecto. Configúralo vía env var (Auth__Jwt__SigningKey).");
        }

        if (options.Jwt.AccessTokenLifetimeMinutes is < 5 or > 24 * 60)
        {
            failures.Add("Auth:Jwt:AccessTokenLifetimeMinutes debe estar entre 5 y 1440.");
        }

        if (options.Users.Count > 0)
        {
            foreach (var user in options.Users)
            {
                var hasPlain = !string.IsNullOrWhiteSpace(user.Password);
                var hasHash = !string.IsNullOrWhiteSpace(user.PasswordHash);

                if (!hasPlain && !hasHash)
                {
                    failures.Add($"Auth:Users[{user.Email}]: Password o PasswordHash es requerido.");
                    continue;
                }

                if (hasHash && !PasswordHasher.TryParse(user.PasswordHash!, out _, out _, out _))
                {
                    failures.Add($"Auth:Users[{user.Email}]: PasswordHash no tiene formato válido (pbkdf2_sha256$iterations$salt$hash)."
                    );
                }
            }
        }

        if (string.IsNullOrWhiteSpace(options.Cookie.AccessTokenCookieName))
            failures.Add("Auth:Cookie:AccessTokenCookieName es requerido.");
        if (string.IsNullOrWhiteSpace(options.Cookie.CsrfCookieName))
            failures.Add("Auth:Cookie:CsrfCookieName es requerido.");
        if (string.IsNullOrWhiteSpace(options.Cookie.CsrfHeaderName))
            failures.Add("Auth:Cookie:CsrfHeaderName es requerido.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
