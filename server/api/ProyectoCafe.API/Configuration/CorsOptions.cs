using Microsoft.Extensions.Configuration;

namespace ProyectoCafe.API.Configuration;

/// <summary>
/// Nombres de las políticas CORS registradas por la API.
/// </summary>
public static class CorsPolicyNames
{
    public const string Portal = "Portal";
}

/// <summary>
/// Configuración CORS de la API. La API es la única autoridad CORS del sistema:
/// el Portal (Next.js) no debe emitir cabeceras Access-Control-*.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Orígenes exactos autorizados (esquema + host + puerto, sin ruta).
    /// No se admite "*" porque la autenticación usa cookies.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}

/// <summary>
/// Resuelve y valida la lista de orígenes CORS antes de registrar la política.
/// </summary>
public static class CorsOrigins
{
    /// <summary>
    /// Alternativa para despliegues en contenedores: lista separada por comas o punto y coma.
    /// Si existe la sección estándar <c>Cors:AllowedOrigins</c>, esta tiene prioridad.
    /// </summary>
    public const string EnvironmentVariable = "CORS_ALLOWED_ORIGINS";

    public static string[] Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration
            .GetSection($"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)}")
            .Get<string[]>();

        var values = configured is { Length: > 0 }
            ? configured
            : (configuration[EnvironmentVariable] ?? string.Empty)
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var origins = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var origin = Normalize(value);
            if (seen.Add(origin))
            {
                origins.Add(origin);
            }
        }

        if (origins.Count == 0)
        {
            throw new InvalidOperationException(
                $"CORS no está configurado. Define '{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)}' " +
                $"o la variable de entorno '{EnvironmentVariable}' con al menos un origen exacto. " +
                "No se permite '*' cuando la autenticación usa cookies.");
        }

        return origins.ToArray();
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Contains('*'))
        {
            throw new InvalidOperationException(
                $"El origen CORS '{value}' no es válido: no se admiten comodines ('*') cuando se envían credenciales. " +
                "Configura cada origen de forma explícita.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"El origen CORS '{value}' no es una URL absoluta http/https válida.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                $"El origen CORS '{value}' debe contener solo esquema, host y puerto " +
                "(sin ruta, query, fragmento ni credenciales).");
        }

        // El navegador envía el origen en minúsculas y sin barra final.
        return uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
    }
}
