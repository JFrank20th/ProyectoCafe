using System.Net;

namespace ProyectoCafe.API.Auth;

internal static class LoginSecurityLog
{
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
}
