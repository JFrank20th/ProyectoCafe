namespace ProyectoCafe.Application.Abstractions.Security;

public enum LoginRateLimitScope
{
    None = 0,
    Ip = 1,
    User = 2,
}

public readonly record struct LoginRateLimitDecision(
    bool IsAllowed,
    LoginRateLimitScope Scope,
    int FailedAttempts,
    TimeSpan RetryAfter)
{
    public static LoginRateLimitDecision Allowed(int failedAttempts = 0) =>
        new(true, LoginRateLimitScope.None, failedAttempts, TimeSpan.Zero);

    public static LoginRateLimitDecision Blocked(LoginRateLimitScope scope, int failedAttempts, TimeSpan retryAfter) =>
        new(false, scope, failedAttempts, retryAfter);
}

public interface ILoginRateLimiter
{
    /// <summary>
    /// Evalúa si la IP y el email pueden intentar un login en este momento.
    /// No registra el intento; el caso de uso debe llamar a <see cref="RecordFailure"/> o <see cref="RecordSuccess"/>.
    /// </summary>
    LoginRateLimitDecision Check(string clientIp, string normalizedEmail);

    void RecordFailure(string clientIp, string normalizedEmail);

    /// <summary>
    /// Un login exitoso limpia la ventana del email. La ventana de la IP no se limpia para que
    /// una credencial válida no permita reiniciar el contador de fuerza bruta desde esa IP.
    /// </summary>
    void RecordSuccess(string clientIp, string normalizedEmail);
}
