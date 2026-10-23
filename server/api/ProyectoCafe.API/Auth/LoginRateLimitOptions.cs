namespace ProyectoCafe.API.Auth;

/// <summary>
/// Límites de intentos fallidos de login aplicados en memoria por instancia de la API.
/// El bloqueo persistente de la cuenta (5 intentos / 15 minutos) se mantiene en la base de datos.
/// </summary>
public sealed class LoginRateLimitOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>Máximo de intentos fallidos permitidos desde una misma IP dentro de la ventana.</summary>
    public int IpMaxFailedAttempts { get; init; } = 20;

    public int IpWindowMinutes { get; init; } = 5;

    /// <summary>Máximo de intentos fallidos permitidos contra un mismo email dentro de la ventana.</summary>
    public int UserMaxFailedAttempts { get; init; } = 10;

    public int UserWindowMinutes { get; init; } = 15;

    /// <summary>
    /// Tope de claves en memoria para evitar crecimiento ilimitado ante intentos con emails aleatorios.
    /// Al superarse se descartan las ventanas menos recientes.
    /// </summary>
    public int MaxTrackedKeys { get; init; } = 10_000;
}
