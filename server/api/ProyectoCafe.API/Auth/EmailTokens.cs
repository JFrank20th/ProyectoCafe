using System.Security.Cryptography;
using System.Text;

namespace ProyectoCafe.API.Auth;

public static class EmailTokens
{
    public static string CreateToken(int bytes = 32)
    {
        var raw = RandomNumberGenerator.GetBytes(bytes);
        return Base64UrlEncode(raw);
    }

    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
