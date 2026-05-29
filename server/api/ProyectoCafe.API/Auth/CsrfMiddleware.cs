using Microsoft.Extensions.Options;

namespace ProyectoCafe.API.Auth;

/// <summary>
/// Minimal CSRF protection for cookie-based auth:
/// - For non-idempotent methods, require header X-CSRF to match csrf cookie.
/// </summary>
public sealed class CsrfMiddleware(RequestDelegate next, IOptions<AuthOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly AuthOptions _options = options.Value;

    public async Task Invoke(HttpContext context)
    {
        var method = context.Request.Method;
        var isUnsafe = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));

        if (!isUnsafe)
        {
            await _next(context);
            return;
        }

        // If not authenticated, let auth handle 401.
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var cookieName = _options.Cookie.CsrfCookieName;
        var headerName = _options.Cookie.CsrfHeaderName;

        context.Request.Cookies.TryGetValue(cookieName, out var csrfCookie);
        var csrfHeader = context.Request.Headers[headerName].ToString();

        if (string.IsNullOrWhiteSpace(csrfCookie) || string.IsNullOrWhiteSpace(csrfHeader) ||
            !string.Equals(csrfCookie, csrfHeader, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "CSRF", message = "CSRF token inválido o faltante." }
            });
            return;
        }

        await _next(context);
    }
}
