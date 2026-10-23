namespace ProyectoCafe.Application.DTOs.Authentication;

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
    // El JWT solo vive en la cookie HttpOnly; el cuerpo únicamente expone
    // los datos que el Portal necesita para pintar la sesión.
    public required UserInfo User { get; set; }
}

public sealed class UserInfo
{
    public required string Email { get; set; }
    public required string Name { get; set; }
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Sesión autenticada con el identificador interno, necesario para emitir el JWT.
/// No se serializa directamente en las respuestas.
/// </summary>
public sealed record AuthenticatedSession(int UserId, UserInfo User);
