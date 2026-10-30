using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class UnitOfWork(ProyectoCafeDbContext db) : IUnitOfWork
{
    private readonly ProyectoCafeDbContext _db = db;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "La operación no se pudo completar porque el registro fue modificado por otra solicitud.",
                ex);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConcurrencyConflictException(
                "La operación no se pudo completar porque otra solicitud creó un registro con los mismos datos.",
                ex);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct)
    {
        if (!_db.Database.IsRelational() ||
            string.Equals(
                _db.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal) ||
            _db.Database.CurrentTransaction is not null)
        {
            return await operation(ct);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sqlException)
        {
            return false;
        }

        for (var index = 0; index < sqlException.Errors.Count; index++)
        {
            if (sqlException.Errors[index].Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}
