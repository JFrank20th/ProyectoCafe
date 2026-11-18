using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProyectoCafe.Infrastructure.Data;

public sealed class ProyectoCafeDbContextFactory : IDesignTimeDbContextFactory<ProyectoCafeDbContext>
{
    public ProyectoCafeDbContext CreateDbContext(string[] args)
    {
        // La cadena de conexión se toma de la variable de entorno Database__ConnectionString
        // (Azure SQL en producción/QA). Si no está definida, se mantiene LocalDB para
        // desarrollo local y creación de migraciones sin secretos commiteados.
        var connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=(localdb)\\mssqllocaldb;Database=ProyectoCafeDesignTime;Trusted_Connection=True;TrustServerCertificate=True";
        }

        var optionsBuilder = new DbContextOptionsBuilder<ProyectoCafeDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new ProyectoCafeDbContext(optionsBuilder.Options);
    }
}