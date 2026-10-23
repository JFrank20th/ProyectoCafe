using Microsoft.Extensions.Options;

namespace ProyectoCafe.API.Auth;

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
    /// No registra el intento; el endpoint debe llamar a <see cref="RecordFailure"/> o <see cref="RecordSuccess"/>.
    /// </summary>
    LoginRateLimitDecision Check(string clientIp, string normalizedEmail);

    void RecordFailure(string clientIp, string normalizedEmail);

    /// <summary>
    /// Un login exitoso limpia la ventana del email. La ventana de la IP no se limpia para que
    /// una credencial válida no permita reiniciar el contador de fuerza bruta desde esa IP.
    /// </summary>
    void RecordSuccess(string clientIp, string normalizedEmail);
}

/// <summary>
/// Ventana deslizante en memoria de intentos fallidos por IP y por email.
/// Es suficiente para la arquitectura actual (una instancia). En un despliegue con varias
/// instancias se debe reemplazar por un almacén distribuido (por ejemplo Redis).
/// </summary>
public sealed class LoginRateLimiter(
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    ILogger<LoginRateLimiter> logger) : ILoginRateLimiter
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly LoginRateLimitOptions _options = options.Value.RateLimit;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<LoginRateLimiter> _logger = logger;
    private readonly object _gate = new();
    private readonly Dictionary<string, FailedAttemptWindow> _windows = new(StringComparer.Ordinal);
    private DateTimeOffset _lastSweep = DateTimeOffset.MinValue;

    public LoginRateLimitDecision Check(string clientIp, string normalizedEmail)
    {
        if (!_options.Enabled)
        {
            return LoginRateLimitDecision.Allowed();
        }

        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            SweepIfNeeded(now);

            var ipDecision = Evaluate(
                BuildIpKey(clientIp),
                LoginRateLimitScope.Ip,
                _options.IpMaxFailedAttempts,
                TimeSpan.FromMinutes(_options.IpWindowMinutes),
                now);

            if (!ipDecision.IsAllowed)
            {
                return ipDecision;
            }

            var userDecision = Evaluate(
                BuildUserKey(normalizedEmail),
                LoginRateLimitScope.User,
                _options.UserMaxFailedAttempts,
                TimeSpan.FromMinutes(_options.UserWindowMinutes),
                now);

            if (!userDecision.IsAllowed)
            {
                return userDecision;
            }

            return LoginRateLimitDecision.Allowed(Math.Max(ipDecision.FailedAttempts, userDecision.FailedAttempts));
        }
    }

    public void RecordFailure(string clientIp, string normalizedEmail)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            SweepIfNeeded(now);

            AddFailure(
                BuildIpKey(clientIp),
                _options.IpMaxFailedAttempts,
                TimeSpan.FromMinutes(_options.IpWindowMinutes),
                now);

            AddFailure(
                BuildUserKey(normalizedEmail),
                _options.UserMaxFailedAttempts,
                TimeSpan.FromMinutes(_options.UserWindowMinutes),
                now);
        }
    }

    public void RecordSuccess(string clientIp, string normalizedEmail)
    {
        if (!_options.Enabled)
        {
            return;
        }

        lock (_gate)
        {
            // El login exitoso demuestra control de la cuenta: se limpia su ventana.
            // No se limpia la IP, para no permitir reiniciar el límite con una cuenta válida.
            _windows.Remove(BuildUserKey(normalizedEmail));
        }
    }

    private LoginRateLimitDecision Evaluate(
        string key,
        LoginRateLimitScope scope,
        int maxAttempts,
        TimeSpan window,
        DateTimeOffset now)
    {
        if (!_windows.TryGetValue(key, out var state))
        {
            return LoginRateLimitDecision.Allowed();
        }

        Prune(state, now);

        if (state.Attempts.Count == 0)
        {
            _windows.Remove(key);
            return LoginRateLimitDecision.Allowed();
        }

        state.LastSeen = now;

        if (state.Attempts.Count < maxAttempts)
        {
            return LoginRateLimitDecision.Allowed(state.Attempts.Count);
        }

        var retryAt = state.Attempts.Peek() + window;
        var retryAfter = retryAt > now ? retryAt - now : TimeSpan.FromSeconds(1);

        return LoginRateLimitDecision.Blocked(scope, state.Attempts.Count, retryAfter);
    }

    private void AddFailure(string key, int maxAttempts, TimeSpan window, DateTimeOffset now)
    {
        if (!_windows.TryGetValue(key, out var state))
        {
            EnsureCapacity(now);

            state = new FailedAttemptWindow(window);
            _windows[key] = state;
        }

        Prune(state, now);

        // Una vez alcanzado el límite no se siguen acumulando marcas de tiempo.
        if (state.Attempts.Count < maxAttempts)
        {
            state.Attempts.Enqueue(now);
        }

        state.LastSeen = now;
    }

    private void EnsureCapacity(DateTimeOffset now)
    {
        if (_windows.Count < _options.MaxTrackedKeys)
        {
            return;
        }

        Sweep(now, force: true);

        var removed = 0;

        while (_windows.Count >= _options.MaxTrackedKeys)
        {
            string? oldestKey = null;
            var oldestSeen = DateTimeOffset.MaxValue;

            foreach (var (key, state) in _windows)
            {
                if (state.LastSeen < oldestSeen)
                {
                    oldestSeen = state.LastSeen;
                    oldestKey = key;
                }
            }

            if (oldestKey is null)
            {
                break;
            }

            _windows.Remove(oldestKey);
            removed++;
        }

        if (removed > 0)
        {
            _logger.LogWarning(
                "Se descartaron {Cantidad} ventanas de rate limiting por límite de claves en memoria. Limite={Limite}",
                removed,
                _options.MaxTrackedKeys);
        }
    }

    private void SweepIfNeeded(DateTimeOffset now)
    {
        if (now - _lastSweep < SweepInterval && _windows.Count < _options.MaxTrackedKeys)
        {
            return;
        }

        Sweep(now, force: false);
    }

    private void Sweep(DateTimeOffset now, bool force)
    {
        if (!force && now - _lastSweep < SweepInterval)
        {
            return;
        }

        _lastSweep = now;

        var keys = new List<string>(_windows.Keys);
        foreach (var key in keys)
        {
            if (!_windows.TryGetValue(key, out var state))
            {
                continue;
            }

            Prune(state, now);

            if (state.Attempts.Count == 0 || now - state.LastSeen >= state.Window)
            {
                _windows.Remove(key);
            }
        }
    }

    private static void Prune(FailedAttemptWindow state, DateTimeOffset now)
    {
        while (state.Attempts.Count > 0 && now - state.Attempts.Peek() >= state.Window)
        {
            state.Attempts.Dequeue();
        }
    }

    private static string BuildIpKey(string clientIp)
    {
        var normalized = string.IsNullOrWhiteSpace(clientIp) ? "desconocida" : clientIp.Trim();
        return "ip:" + normalized;
    }

    private static string BuildUserKey(string normalizedEmail)
    {
        var email = (normalizedEmail ?? string.Empty).Trim().ToLowerInvariant();
        return "user:" + (email.Length == 0 ? "(vacio)" : email);
    }

    private sealed class FailedAttemptWindow(TimeSpan window)
    {
        public TimeSpan Window { get; } = window;

        public Queue<DateTimeOffset> Attempts { get; } = new();

        public DateTimeOffset LastSeen { get; set; }
    }
}
