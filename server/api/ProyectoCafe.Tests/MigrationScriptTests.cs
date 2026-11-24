using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

/// <summary>
/// Evita que el script oficial de despliegue vuelva a quedar desactualizado
/// respecto de las migraciones compiladas en Infrastructure.
/// </summary>
public sealed class MigrationScriptTests
{
    private const string ScriptRelativePath = "tools/sqlserver-migrate-current.sql";

    [Fact]
    public void OfficialScript_ContainsEveryKnownMigration()
    {
        var options = new DbContextOptionsBuilder<ProyectoCafeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ProyectoCafeDesignTime;" +
                "Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var db = new ProyectoCafeDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();

        Assert.NotEmpty(migrations);

        var scriptPath = FindRepositoryFile(ScriptRelativePath);
        var script = File.ReadAllText(scriptPath);

        foreach (var migration in migrations)
        {
            Assert.Contains(migration, script, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, normalized);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"No se encontró '{relativePath}' partiendo de '{AppContext.BaseDirectory}'.");
    }
}
