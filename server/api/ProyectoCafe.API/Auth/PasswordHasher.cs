using System.Security.Cryptography;

namespace ProyectoCafe.API.Auth;

public static class PasswordHasher
{
    // Format: pbkdf2_sha256$<iterations>$<saltBase64>$<hashBase64>
    private const string Prefix = "pbkdf2_sha256";

    public static string HashPassword(string password, int iterations = 100_000, int saltSize = 16, int hashSize = 32)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        if (iterations < 10_000) throw new ArgumentOutOfRangeException(nameof(iterations));

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

    public static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = passwordHash.Split('$', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        if (!string.Equals(parts[0], Prefix, StringComparison.Ordinal)) return false;
        if (!int.TryParse(parts[1], out iterations) || iterations < 10_000) return false;

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
