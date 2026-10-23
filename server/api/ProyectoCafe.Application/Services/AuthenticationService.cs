using Microsoft.Extensions.Logging;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Abstractions.Security;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Authentication;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

/// <summary>
/// Casos de uso de autenticación: registro, inicio de sesión, renovación de
/// sesión y datos de la sesión actual. La emisión del JWT y las cookies queda
/// en la capa API por ser un detalle de transporte.
/// </summary>
public sealed class AuthenticationService(
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    ILoginRateLimiter rateLimiter,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider)
{
    // Se conserva la categoría histórica para no romper las alertas ni las
    // pruebas que supervisan los eventos de seguridad del login.
    private const string LoginLogCategory = "ProyectoCafe.API.Auth.Login";

    private readonly IUsuarioRepository _usuarios = usuarios;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILoginRateLimiter _rateLimiter = rateLimiter;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<OperationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct)
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
            return OperationResult<RegisterResponse>.Validation(errors);
        }

        var emailNormalizado = email;

        var exists = await _usuarios.EmailExistsAsync(emailNormalizado, ct);
        if (exists)
        {
            return OperationResult<RegisterResponse>.Validation(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] = ["Este email ya está registrado."]
            });
        }

        var nombre = string.IsNullOrWhiteSpace(nameRaw) ? email : nameRaw;
        if (nombre.Length > 150) nombre = nombre[..150];

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var user = new Usuario
        {
            Email = email,
            EmailNormalizado = emailNormalizado,
            Nombre = nombre,
            PasswordHash = PasswordHasher.HashPassword(password),
            Estado = UsuarioEstado.PendienteActivacion,
            CreadoEn = now,
        };

        await _usuarios.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<RegisterResponse>.Success(new RegisterResponse
        {
            Message = "Registro creado. Tu cuenta está pendiente de activación por un administrador."
        });
    }

    public async Task<LoginOutcome> LoginAsync(
        LoginRequest request,
        string clientIp,
        CancellationToken ct)
    {
        var logger = _loggerFactory.CreateLogger(LoginLogCategory);
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = (request.Password ?? string.Empty).Trim();
        var maskedEmail = LoginSecurityLog.MaskEmail(email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return LoginOutcome.ValidationFailed(new Dictionary<string, string[]>
            {
                ["request"] = ["Email y password son requeridos."]
            });
        }

        // Mismo máximo que el registro y la columna EmailNormalizado.
        if (email.Length > 100 || password.Length > 100)
        {
            return InvalidCredentials();
        }

        var limit = _rateLimiter.Check(clientIp, email);
        if (!limit.IsAllowed)
        {
            logger.LogWarning(
                "Login bloqueado por rate limiting. Alcance={Alcance} Ip={Ip} Email={Email} IntentosFallidos={IntentosFallidos} ReintentarEnSegundos={ReintentarEnSegundos}",
                limit.Scope,
                clientIp,
                maskedEmail,
                limit.FailedAttempts,
                (int)Math.Ceiling(limit.RetryAfter.TotalSeconds));

            return TooManyRequests(limit.RetryAfter);
        }

        var user = await _usuarios.FindByNormalizedEmailAsync(email, ct);

        if (user is null)
        {
            _rateLimiter.RecordFailure(clientIp, email);
            logger.LogWarning(
                "Login fallido: usuario inexistente. Ip={Ip} Email={Email}",
                clientIp,
                maskedEmail);

            return InvalidCredentials();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (user.LockoutFin is not null && user.LockoutFin > now)
        {
            var retryAfter = user.LockoutFin.Value - now;

            logger.LogWarning(
                "Login bloqueado: cuenta con bloqueo temporal vigente. Ip={Ip} Email={Email} UsuarioId={UsuarioId} BloqueadaHasta={BloqueadaHasta}",
                clientIp,
                maskedEmail,
                user.UsuarioId,
                user.LockoutFin);

            return TooManyRequests(retryAfter);
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

            return LoginOutcome.UserNotActive("USER_NOT_ACTIVE", message);
        }

        var ok = PasswordHasher.Verify(password, user.PasswordHash);
        if (!ok)
        {
            _rateLimiter.RecordFailure(clientIp, email);

            user.IntentosFallidosLogin += 1;
            var lockoutTriggered = false;
            if (user.IntentosFallidosLogin >= 5)
            {
                user.LockoutFin = now.AddMinutes(15);
                lockoutTriggered = true;
            }
            user.ActualizadoEn = now;
            await _unitOfWork.SaveChangesAsync(ct);

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

        // Migra hashes antiguos (menos iteraciones o salt pequeño) al
        // esquema recomendado sin cambiar la contraseña del usuario.
        if (PasswordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.HashPassword(password);
        }

        user.IntentosFallidosLogin = 0;
        user.LockoutFin = null;
        user.UltimoLoginEn = now;
        user.ActualizadoEn = now;

        var (roles, permissions) = await LoadUserAccessAsync(user.UsuarioId, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _rateLimiter.RecordSuccess(clientIp, email);

        logger.LogInformation(
            "Login exitoso. Ip={Ip} Email={Email} UsuarioId={UsuarioId}",
            clientIp,
            maskedEmail,
            user.UsuarioId);

        return LoginOutcome.Success(new UserInfo
        {
            Email = user.Email,
            Name = user.Nombre,
            Roles = roles,
            Permissions = permissions,
        }, user.UsuarioId);
    }

    public async Task<AuthenticatedSession?> RefreshAsync(int userId, CancellationToken ct)
    {
        var user = await _usuarios.FindActiveByIdAsync(userId, ct);
        if (user is null)
        {
            return null;
        }

        var (roles, permissions) = await LoadUserAccessAsync(user.UsuarioId, ct);

        return new AuthenticatedSession(user.UsuarioId, new UserInfo
        {
            Email = user.Email,
            Name = user.Nombre,
            Roles = roles,
            Permissions = permissions,
        });
    }

    public async Task<UserInfo> GetCurrentUserAsync(
        int userId,
        string email,
        string name,
        CancellationToken ct)
    {
        var (roles, permissions) = await LoadUserAccessAsync(userId, ct);

        return new UserInfo
        {
            Email = email,
            Name = name,
            Roles = roles,
            Permissions = permissions,
        };
    }

    private async Task<(string[] Roles, string[] Permissions)> LoadUserAccessAsync(
        int userId,
        CancellationToken ct)
    {
        var roles = await _usuarios.GetActiveRoleNamesAsync(userId, ct);
        var permissions = await _usuarios.GetActivePermissionPoliciesAsync(
            userId,
            PermissionKey.PolicyPrefix,
            ct);

        return (roles, permissions);
    }

    private static LoginOutcome InvalidCredentials() =>
        LoginOutcome.InvalidCredentials("INVALID_CREDENTIALS", "Email o contraseña incorrectos.");

    private static LoginOutcome TooManyRequests(TimeSpan retryAfter) =>
        LoginOutcome.TooManyRequests(
            retryAfter,
            "TOO_MANY_REQUESTS",
            "Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.");
}
