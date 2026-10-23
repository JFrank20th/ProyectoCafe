using System.Net;
using System.Text;

namespace ProyectoCafe.API.Auth;

internal static class LoginSecurityLog
{
    private const int MaxEmailLength = 320;

    public static string GetClientIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null)
        {
            return "desconocida";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        return ip.ToString();
    }

    /// <summary>
    /// Enmascara el email para las trazas de seguridad y elimina caracteres de control
    /// para evitar inyección de líneas en los logs.
    /// </summary>
    public static string MaskEmail(string? email)
    {
        var sanitized = Sanitize(email);
        if (sanitized.Length == 0)
        {
            return "(vacio)";
        }

        var at = sanitized.IndexOf('@');
        if (at <= 0)
        {
            return sanitized[..1] + "***";
        }

        var local = sanitized[..at];
        var domain = sanitized[(at + 1)..];
        return $"{local[..1]}***@{domain}";
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(value.Length, MaxEmailLength));
        foreach (var c in value.Trim())
        {
            if (builder.Length >= MaxEmailLength)
            {
                break;
            }

            builder.Append(char.IsControl(c) ? '?' : c);
        }

        return builder.ToString();
    }
}
