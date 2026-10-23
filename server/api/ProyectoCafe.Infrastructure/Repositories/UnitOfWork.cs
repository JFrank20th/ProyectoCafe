using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Infrastructure.Repositories;

public sealed class UnitOfWork(ProyectoCafeDbContext db) : IUnitOfWork
{
    private readonly ProyectoCafeDbContext _db = db;

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
