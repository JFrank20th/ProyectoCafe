namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
