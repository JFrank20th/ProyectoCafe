using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProyectoCafe.Infrastructure.Data;

public sealed class ProyectoCafeDbContextFactory : IDesignTimeDbContextFactory<ProyectoCafeDbContext>
{
    public ProyectoCafeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProyectoCafeDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ProyectoCafeDesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new ProyectoCafeDbContext(optionsBuilder.Options);
    }
}