using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

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
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var password = (request.Password ?? string.Empty).Trim();
            var nameRaw = (request.Name ?? string.Empty).Trim();

            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors[nameof(request.Email)] = ["Email es requerido."];
            }
            else if (email.Length > 100)
            {
                errors[nameof(request.Email)] = ["Email no puede exceder 100 caracteres."];
            }
            else if (!email.Contains('@', StringComparison.Ordinal))
            {
                errors[nameof(request.Email)] = ["Email no es válido."];
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors[nameof(request.Password)] = ["Password es requerido."];
            }
            else if (password.Length < 8)
            {
                errors[nameof(request.Password)] = ["Password debe tener al menos 8 caracteres."];
            }
            else if (password.Length > 100)
            {
                errors[nameof(request.Password)] = ["Password no puede exceder 100 caracteres."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var emailNormalizado = email;

            var exists = await db.Usuarios
                .AnyAsync(u => u.EmailNormalizado == emailNormalizado && u.Estado != UsuarioEstado.Eliminado, ct);
            if (exists)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Email)] = ["Este email ya está registrado."]
                });
            }

            var nombre = string.IsNullOrWhiteSpace(nameRaw) ? email : nameRaw;
            if (nombre.Length > 150) nombre = nombre[..150];

            var now = DateTime.UtcNow;

            var user = new Usuario
            {
                Email = email,
                EmailNormalizado = emailNormalizado,
                Nombre = nombre,
                PasswordHash = PasswordHasher.HashPassword(password),
                Estado = UsuarioEstado.PendienteActivacion,
                CreadoEn = now,
            };

            db.Usuarios.Add(user);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new RegisterResponse
            {
                Message = "Registro creado. Tu cuenta está pendiente de activación por un administrador."
            });
        })
        .AllowAnonymous();

        group.MapPost("/login", async (
            HttpContext http,
            [FromBody] LoginRequest request,
            [FromServices] ProyectoCafeDbContext db,
            [FromServices] IOptions<AuthOptions> options,
            [FromServices] IJwtTokenService jwtTokenService,
            [FromServices] ILoginRateLimiter rateLimiter,
            [FromServices] ILoggerFactory loggerFactory,
            [FromServices] IHostEnvironment environment,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("ProyectoCafe.API.Auth.Login");
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var password = (request.Password ?? string.Empty).Trim();
            var clientIp = LoginSecurityLog.GetClientIp(http);
            var maskedEmail = LoginSecurityLog.MaskEmail(email);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Email y password son requeridos."] });
            }

            // Mismo máximo que el registro y la columna EmailNormalizado.
            if (email.Length > 100 || password.Length > 100)
            {
                return InvalidCredentials();
            }

            var limit = rateLimiter.Check(clientIp, email);
            if (!limit.IsAllowed)
            {
                logger.LogWarning(
                    "Login bloqueado por rate limiting. Alcance={Alcance} Ip={Ip} Email={Email} IntentosFallidos={IntentosFallidos} ReintentarEnSegundos={ReintentarEnSegundos}",
                    limit.Scope,
                    clientIp,
                    maskedEmail,
                    limit.FailedAttempts,
                    (int)Math.Ceiling(limit.RetryAfter.TotalSeconds));

                return TooManyRequests(http, limit.RetryAfter);
            }

            var user = await db.Usuarios
                .FirstOrDefaultAsync(u => u.EmailNormalizado == email && u.Estado != UsuarioEstado.Eliminado, ct);

            if (user is null)
            {
                rateLimiter.RecordFailure(clientIp, email);
                logger.LogWarning(
                    "Login fallido: usuario inexistente. Ip={Ip} Email={Email}",
                    clientIp,
                    maskedEmail);

                return InvalidCredentials();
            }

            var now = DateTime.UtcNow;
            if (user.LockoutFin is not null && user.LockoutFin > now)
            {
                var retryAfter = user.LockoutFin.Value - now;

                logger.LogWarning(
                    "Login bloqueado: cuenta con bloqueo temporal vigente. Ip={Ip} Email={Email} UsuarioId={UsuarioId} BloqueadaHasta={BloqueadaHasta}",
                    clientIp,
                    maskedEmail,
                    user.UsuarioId,
                    user.LockoutFin);

                return TooManyRequests(http, retryAfter);
            }

            if (user.Estado != UsuarioEstado.Activo)
            {
                var message = user.Estado switch
                {
                    UsuarioEstado.PendienteActivacion => "Tu cuenta está pendiente de activación por un administrador.",
                    UsuarioEstado.Inactivo => "Tu cuenta está desactivada.",
                    _ => "Tu cuenta no está activa."
                };

                logger.LogWarning(
                    "Login rechazado por estado de cuenta. Ip={Ip} Email={Email} UsuarioId={UsuarioId} Estado={Estado}",
                    clientIp,
                    maskedEmail,
                    user.UsuarioId,
                    user.Estado);

                return Results.Json(new
                {
                    error = new { code = "USER_NOT_ACTIVE", message }
                }, statusCode: StatusCodes.Status403Forbidden);
            }

            var ok = PasswordHasher.Verify(password, user.PasswordHash);
            if (!ok)
            {
                rateLimiter.RecordFailure(clientIp, email);

                user.IntentosFallidosLogin += 1;
                var lockoutTriggered = false;
                if (user.IntentosFallidosLogin >= 5)
                {
                    user.LockoutFin = now.AddMinutes(15);
                    lockoutTriggered = true;
                }
                user.ActualizadoEn = now;
                await db.SaveChangesAsync(ct);

                logger.LogWarning(
                    "Login fallido: credenciales inválidas. Ip={Ip} Email={Email} UsuarioId={UsuarioId} IntentosFallidos={IntentosFallidos} CuentaBloqueada={CuentaBloqueada} BloqueadaHasta={BloqueadaHasta}",
                    clientIp,
                    maskedEmail,
                    user.UsuarioId,
                    user.IntentosFallidosLogin,
                    lockoutTriggered,
                    user.LockoutFin);

                return InvalidCredentials();
            }

            user.IntentosFallidosLogin = 0;
            user.LockoutFin = null;
            user.UltimoLoginEn = now;
            user.ActualizadoEn = now;

            var roles = await db.UsuariosRoles
                .Where(ur => ur.UsuarioId == user.UsuarioId)
                .Join(db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
                .Where(r => r.Estado == RolEstado.Activo)
                .Select(r => r.Nombre)
                .Distinct()
                .ToArrayAsync(ct);

            var permissions = await db.UsuariosRoles
                .Where(ur => ur.UsuarioId == user.UsuarioId)
                .Join(db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
                .Where(r => r.Estado == RolEstado.Activo)
                .Join(db.RolesPermisos, r => r.RolId, rp => rp.RolId, (_, rp) => rp)
                .Select(rp => Permissions.PolicyPrefix + rp.Modulo + ":" + rp.Accion)
                .Distinct()
                .OrderBy(x => x)
                .ToArrayAsync(ct);

            await db.SaveChangesAsync(ct);

            rateLimiter.RecordSuccess(clientIp, email);

            logger.LogInformation(
                "Login exitoso. Ip={Ip} Email={Email} UsuarioId={UsuarioId}",
                clientIp,
                maskedEmail,
                user.UsuarioId);

            var token = jwtTokenService.CreateToken(new JwtUser(user.UsuarioId, user.Email, user.Nombre, roles));

            var csrfToken = AuthCookies.CreateCsrfToken();

            var cookieSecure = !environment.IsDevelopment();

            var accessCookie = new CookieOptions
            {
                HttpOnly = true,
                Secure = cookieSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            };

            var csrfCookie = new CookieOptions
            {
                HttpOnly = false,
                Secure = cookieSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            };

            // Set cookies
            var authCookieName = options.Value.Cookie.AccessTokenCookieName;
            var csrfCookieName = options.Value.Cookie.CsrfCookieName;
            http.Response.Cookies.Append(authCookieName, token, accessCookie);
            http.Response.Cookies.Append(csrfCookieName, csrfToken, csrfCookie);

            return Results.Ok(new LoginResponse
            {
                AccessToken = token,
                User = new UserInfo
                {
                    Email = user.Email,
                    Name = user.Nombre,
                    Roles = roles,
                    Permissions = permissions,
                }
            });
        })
        .AllowAnonymous();

        group.MapPost("/logout", (
            HttpContext http,
            [FromServices] IOptions<AuthOptions> options) =>
        {
            var authCookieName = options.Value.Cookie.AccessTokenCookieName;
            var csrfCookieName = options.Value.Cookie.CsrfCookieName;

            http.Response.Cookies.Delete(authCookieName, new CookieOptions { Path = "/" });
            http.Response.Cookies.Delete(csrfCookieName, new CookieOptions { Path = "/" });

            return Results.NoContent();
        });

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            [FromServices] ProyectoCafeDbContext db,
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

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();

            var permissions = await db.UsuariosRoles
                .Where(ur => ur.UsuarioId == userId)
                .Join(db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
                .Where(r => r.Estado == RolEstado.Activo)
                .Join(db.RolesPermisos, r => r.RolId, rp => rp.RolId, (_, rp) => rp)
                .Select(rp => Permissions.PolicyPrefix + rp.Modulo + ":" + rp.Accion)
                .Distinct()
                .OrderBy(x => x)
                .ToArrayAsync(ct);

            return Results.Ok(new UserInfo { Email = email, Name = name, Roles = roles, Permissions = permissions });
        })
        .RequireAuthorization();

        return endpoints;
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

    public sealed class RegisterRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
    }

    public sealed class RegisterResponse
    {
        public required string Message { get; set; }
    }

    public sealed class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public sealed class LoginResponse
    {
        public required string AccessToken { get; set; }
        public required UserInfo User { get; set; }
    }

    public sealed class UserInfo
    {
        public required string Email { get; set; }
        public required string Name { get; set; }
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    }
}
