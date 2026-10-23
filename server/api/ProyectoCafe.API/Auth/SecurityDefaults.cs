namespace ProyectoCafe.API.Auth;

/// <summary>
/// Reglas compartidas para detectar configuración de seguridad provisional.
/// </summary>
public static class SecurityDefaults
{
    public const string SigningKeyEnvironmentVariable = "Auth__Jwt__SigningKey";

    public const int MinimumSigningKeyBytes = 32;

    private static readonly string[] InsecureSigningKeyMarkers =
    [
        "CHANGE_ME",
        "CHANGEME",
        "CHANGE-ME",
        "DEV_ONLY",
        "DEV-SIGNING",
        "DEV_SIGNING",
        "PLEASE_USE_ENV",
        "PLEASE-CHANGE",
        "REPLACE_ME",
        "REPLACEME",
        "REPLACE-ME",
        "YOUR_SECRET",
        "YOUR-SECRET",
        "YOURSIGNINGKEY",
        "EXAMPLE_KEY",
        "EXAMPLE-SECRET",
        "PROVISIONAL",
        "0123456789",
    ];

    /// <summary>
    /// Devuelve true cuando la clave de firma es vacía, un marcador de ejemplo
    /// o una cadena evidentemente débil (por ejemplo, un solo carácter repetido).
    /// </summary>
    public static bool IsPlaceholderSigningKey(string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return true;
        }

        var value = signingKey.Trim();

        if (InsecureSigningKeyMarkers.Any(marker =>
                value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Claves como "aaaaaaaa...", "12341234..." o "abababab..." no aportan
        // entropía suficiente aunque cumplan la longitud mínima.
        return value.Distinct().Count() < 8;
    }

    /// <summary>
    /// Detecta las cuentas de demostración históricas (admin@local/admin y
    /// user@local/user), incluso si el hash ya está almacenado en la base de
    /// datos de un despliegue anterior.
    /// </summary>
    public static bool IsKnownDefaultCredential(string? email, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var defaultPassword = normalizedEmail switch
        {
            "admin@local" => "admin",
            "user@local" => "user",
            _ => null,
        };

        if (defaultPassword is null)
        {
            return false;
        }

        // Cubre tanto un valor legado en texto plano como el hash PBKDF2 que
        // generaba el bootstrap anterior al hashear "admin"/"user".
        return string.Equals(passwordHash, defaultPassword, StringComparison.Ordinal)
            || PasswordHasher.Verify(defaultPassword, passwordHash);
    }
}
