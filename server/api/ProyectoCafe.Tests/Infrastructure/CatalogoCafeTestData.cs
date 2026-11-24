using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests.Infrastructure;

/// <summary>
/// Datos de catálogo compartidos por las pruebas que crean lotes de café.
/// </summary>
internal static class CatalogoCafeTestData
{
    public static async Task<(int VariedadCafeId, int TipoTuesteCafeId, int TipoProcesoCafeId)>
        SeedLoteCafeAsync(ProyectoCafeDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var now = DateTime.UtcNow;

        var variedad = new VariedadCafe
        {
            Codigo = $"VAR-{suffix}",
            Nombre = $"Variedad {suffix}",
            Estado = EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };
        var tueste = new TipoTuesteCafe
        {
            Codigo = $"TUE-{suffix}",
            Nombre = $"Tueste {suffix}",
            Estado = EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };
        var proceso = new TipoProcesoCafe
        {
            Codigo = $"PRO-{suffix}",
            Nombre = $"Proceso {suffix}",
            Estado = EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        db.VariedadesCafe.Add(variedad);
        db.TiposTuesteCafe.Add(tueste);
        db.TiposProcesoCafe.Add(proceso);
        await db.SaveChangesAsync();

        return (variedad.VariedadCafeId, tueste.TipoTuesteCafeId, proceso.TipoProcesoCafeId);
    }
}
