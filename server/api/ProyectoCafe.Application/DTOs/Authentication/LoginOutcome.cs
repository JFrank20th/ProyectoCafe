namespace ProyectoCafe.Application.DTOs.Authentication;

public enum LoginOutcomeStatus
{
    Success,
    ValidationFailed,
    InvalidCredentials,
    TooManyRequests,
    UserNotActive,
}

/// <summary>
/// Resultado del caso de uso de inicio de sesión. Incluye la información que
/// la capa API necesita para construir la respuesta HTTP y las cookies.
/// </summary>
public sealed class LoginOutcome
{
    private LoginOutcome(
        LoginOutcomeStatus status,
        UserInfo? user = null,
        int? userId = null,
        Dictionary<string, string[]>? validationErrors = null,
        string? errorCode = null,
        string? errorMessage = null,
        TimeSpan? retryAfter = null)
    {
        Status = status;
        User = user;
        UserId = userId;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RetryAfter = retryAfter;
    }

    public LoginOutcomeStatus Status { get; }

    public UserInfo? User { get; }

    public int? UserId { get; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public TimeSpan? RetryAfter { get; }

    public static LoginOutcome Success(UserInfo user, int userId) =>
        new(LoginOutcomeStatus.Success, user: user, userId: userId);

    public static LoginOutcome ValidationFailed(Dictionary<string, string[]> errors) =>
        new(LoginOutcomeStatus.ValidationFailed, validationErrors: errors);

    public static LoginOutcome InvalidCredentials(string code, string message) =>
        new(LoginOutcomeStatus.InvalidCredentials, errorCode: code, errorMessage: message);

    public static LoginOutcome TooManyRequests(TimeSpan retryAfter, string code, string message) =>
        new(LoginOutcomeStatus.TooManyRequests, errorCode: code, errorMessage: message, retryAfter: retryAfter);

    public static LoginOutcome UserNotActive(string code, string message) =>
        new(LoginOutcomeStatus.UserNotActive, errorCode: code, errorMessage: message);
}
