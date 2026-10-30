namespace ProyectoCafe.Application.Common;

/// <summary>
/// Se produce cuando la persistencia detecta que otra operación modificó el
/// mismo registro o violó una restricción única durante una escritura.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
