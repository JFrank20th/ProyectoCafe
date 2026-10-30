namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Ejecuta una operación de persistencia dentro de una transacción cuando el
    /// proveedor lo soporta. La implementación de Infrastructure decide cómo
    /// abrir, confirmar o revertir la transacción.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct);
}
