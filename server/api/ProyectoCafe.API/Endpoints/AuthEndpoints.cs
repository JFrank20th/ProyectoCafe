using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.DTOs.Authentication;
using ProyectoCafe.Application.Services;

namespace ProyectoCafe.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            [FromServices] AuthenticationService authentication,
            CancellationToken ct) =>
        {
            var result = await authentication.RegisterAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(
                    result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));
            }

            return Results.Ok(result.Value);
        })
        .AllowAnonymous();

        group.MapPost("/login", async (
            HttpContext http,
            [FromBody] LoginRequest request,
            [FromServices] AuthenticationService authentication,
            [FromServices] IOptions<AuthOptions> options,
            [FromServices] IJwtTokenService jwtTokenService,
            [FromServices] IHostEnvironment environment,
            CancellationToken ct) =>
        {
            var outcome = await authentication.LoginAsync(
                request,
                LoginSecurityLog.GetClientIp(http),
                ct);

            switch (outcome.Status)
            {
                case LoginOutcomeStatus.ValidationFailed:
                    return Results.ValidationProblem(
                        outcome.ValidationErrors.ToDictionary(pair => pair.Key, pair => pair.Value));

                case LoginOutcomeStatus.InvalidCredentials:
                    return InvalidCredentials();

                case LoginOutcomeStatus.TooManyRequests:
                    return TooManyRequests(http, outcome.RetryAfter ?? TimeSpan.FromSeconds(1));

                case LoginOutcomeStatus.UserNotActive:
                    return Results.Json(new
                    {
                        error = new
                        {
                            code = outcome.ErrorCode,
                            message = outcome.ErrorMessage
                        }
                    }, statusCode: StatusCodes.Status403Forbidden);

                default:
                    var user = outcome.User!;
                    var token = jwtTokenService.CreateToken(
                        new JwtUser(outcome.UserId!.Value, user.Email, user.Name, user.Roles.ToArray()));
                    var csrfToken = AuthCookies.CreateCsrfToken();

                    // El JWT viaja exclusivamente en una cookie HttpOnly. Nunca se
                    // devuelve en el cuerpo de la respuesta ni queda accesible a JS.
                    SetAuthCookies(http, options.Value, environment, token, csrfToken);

                    return Results.Ok(new LoginResponse { User = user });
            }
        })
        .AllowAnonymous();

        group.MapPost("/refresh", async (
            HttpContext http,
            ClaimsPrincipal principal,
            [FromServices] AuthenticationService authentication,
            [FromServices] IOptions<AuthOptions> options,
            [FromServices] IJwtTokenService jwtTokenService,
            [FromServices] IHostEnvironment environment,
            CancellationToken ct) =>
        {
            if (principal.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var uid = principal.FindFirst("uid")?.Value;
            if (!int.TryParse(uid, out var userId))
            {
                return Results.Unauthorized();
            }

            var session = await authentication.RefreshAsync(userId, ct);
            if (session is null)
            {
                return Results.Unauthorized();
            }

            var user = session.User;

            // Renueva el JWT y rota el token CSRF manteniendo la misma sesión.
            var token = jwtTokenService.CreateToken(
                new JwtUser(session.UserId, user.Email, user.Name, user.Roles.ToArray()));
            var csrfToken = AuthCookies.CreateCsrfToken();

            SetAuthCookies(http, options.Value, environment, token, csrfToken);

            return Results.Ok(new LoginResponse { User = user });
        })
        .RequireAuthorization();

        group.MapPost("/logout", (
            HttpContext http,
            [FromServices] IOptions<AuthOptions> options,
            [FromServices] IHostEnvironment environment) =>
        {
            // AllowAnonymous: el logout debe poder limpiar cookies caducadas o
            // inválidas sin depender de que el JWT siga vigente.
            ClearAuthCookies(http, options.Value, environment);

            return Results.NoContent();
        })
        .AllowAnonymous();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            [FromServices] AuthenticationService authentication,
            CancellationToken ct) =>
        {
            if (principal.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var uid = principal.FindFirst("uid")?.Value;
            if (!int.TryParse(uid, out var userId))
            {
                return Results.Unauthorized();
            }

            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? principal.FindFirstValue("sub")
                ?? string.Empty;

            var name = principal.FindFirstValue("name")
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? email;

            var user = await authentication.GetCurrentUserAsync(userId, email, name, ct);

            return Results.Ok(user);
        })
        .RequireAuthorization();

        return endpoints;
    }

    private static CookieOptions CreateCookieOptions(IHostEnvironment environment, bool httpOnly) =>
        new()
        {
            HttpOnly = httpOnly,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
        };

    private static void SetAuthCookies(
        HttpContext http,
        AuthOptions options,
        IHostEnvironment environment,
        string token,
        string csrfToken)
    {
        http.Response.Cookies.Append(
            options.Cookie.AccessTokenCookieName,
            token,
            CreateCookieOptions(environment, httpOnly: true));

        http.Response.Cookies.Append(
            options.Cookie.CsrfCookieName,
            csrfToken,
            CreateCookieOptions(environment, httpOnly: false));
    }

    private static void ClearAuthCookies(HttpContext http, AuthOptions options, IHostEnvironment environment)
    {
        http.Response.Cookies.Delete(
            options.Cookie.AccessTokenCookieName,
            CreateCookieOptions(environment, httpOnly: true));

        http.Response.Cookies.Delete(
            options.Cookie.CsrfCookieName,
            CreateCookieOptions(environment, httpOnly: false));
    }

    private static IResult InvalidCredentials() =>
        Results.Json(new
        {
            error = new
            {
                code = "INVALID_CREDENTIALS",
                message = "Email o contraseña incorrectos."
            }
        }, statusCode: StatusCodes.Status401Unauthorized);

    private static IResult TooManyRequests(HttpContext http, TimeSpan retryAfter)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        return Results.Json(new
        {
            error = new
            {
                code = "TOO_MANY_REQUESTS",
                message = "Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde."
            }
        }, statusCode: StatusCodes.Status429TooManyRequests);
    }
}
