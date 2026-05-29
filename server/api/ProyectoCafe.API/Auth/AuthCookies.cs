using System.Security.Cryptography;

namespace ProyectoCafe.API.Auth;

public static class AuthCookies
{
    public static string CreateCsrfToken(int bytes = 32)
    {
        var raw = RandomNumberGenerator.GetBytes(bytes);
        return Base64UrlEncode(raw);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
