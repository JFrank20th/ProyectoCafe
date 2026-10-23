using System.Security.Cryptography;

namespace ProyectoCafe.Application.Security;

public static class PasswordHasher
{
    // Format: pbkdf2_sha256$<iterations>$<saltBase64>$<hashBase64>
    private const string Prefix = "pbkdf2_sha256";

    /// <summary>
    /// Iteraciones recomendadas por OWASP para PBKDF2-HMAC-SHA256 (2023+).
    /// Los hashes antiguos con menos iteraciones se actualizan al iniciar sesión.
    /// </summary>
    public const int DefaultIterations = 600_000;

    /// <summary>Mínimo aceptado al verificar hashes ya almacenados.</summary>
    public const int MinimumSupportedIterations = 10_000;

    /// <summary>
    /// Mínimo exigido a los hashes definidos en configuración para usuarios
    /// iniciales (Auth:Users). El valor recomendado sigue siendo 600000.
    /// </summary>
    public const int MinimumSeedHashIterations = 100_000;

    private const int MaximumSupportedIterations = 10_000_000;

    public static string HashPassword(string password, int iterations = DefaultIterations, int saltSize = 16, int hashSize = 32)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        if (iterations < MinimumSupportedIterations || iterations > MaximumSupportedIterations)
            throw new ArgumentOutOfRangeException(nameof(iterations));
        if (saltSize < 16) throw new ArgumentOutOfRangeException(nameof(saltSize));
        if (hashSize < 32) throw new ArgumentOutOfRangeException(nameof(hashSize));

        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);

        return string.Join('$', Prefix, iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash)) return false;

        if (!TryParse(passwordHash, out var iterations, out var salt, out var expectedHash)) return false;

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Indica si el hash almacenado debe reemplazarse tras un login correcto
    /// (por ejemplo, porque se generó con menos iteraciones o un salt pequeño).
    /// </summary>
    public static bool NeedsRehash(string passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out var salt, out var hash))
        {
            return true;
        }

        return iterations < DefaultIterations || salt.Length < 16 || hash.Length < 32;
    }

    public static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = passwordHash.Split('$', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        if (!string.Equals(parts[0], Prefix, StringComparison.Ordinal)) return false;
        if (!int.TryParse(parts[1], out iterations) ||
            iterations < MinimumSupportedIterations ||
            iterations > MaximumSupportedIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
            return salt.Length >= 8 && hash.Length >= 16;
        }
        catch
        {
            return false;
        }
    }
}
