namespace ProyectoCafe.Application.Common;

public enum OperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
}

/// <summary>
/// Resultado de un caso de uso. La capa de aplicación no conoce códigos HTTP:
/// la API traduce el estado a la respuesta correspondiente.
/// </summary>
public class OperationResult
{
    protected OperationResult(OperationStatus status, Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public OperationStatus Status { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public bool IsSuccess => Status == OperationStatus.Success;

    public static OperationResult Success() => new(OperationStatus.Success);

    public static OperationResult NotFound() => new(OperationStatus.NotFound);

    public static OperationResult Validation(Dictionary<string, string[]> errors) =>
        new(OperationStatus.ValidationFailed, errors);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(OperationStatus status, T? value, Dictionary<string, string[]>? errors = null)
        : base(status, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(OperationStatus.Success, value);

    public static new OperationResult<T> NotFound() => new(OperationStatus.NotFound, default);

    public static new OperationResult<T> Validation(Dictionary<string, string[]> errors) =>
        new(OperationStatus.ValidationFailed, default, errors);
}
