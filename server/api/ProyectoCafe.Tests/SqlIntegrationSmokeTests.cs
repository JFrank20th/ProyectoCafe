using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

[Collection("SqlIntegration")]
public sealed class SqlIntegrationSmokeTests : IClassFixture<ProyectoCafeSqlIntegrationFactory>
{
    private readonly ProyectoCafeSqlIntegrationFactory _factory;

    public SqlIntegrationSmokeTests(ProyectoCafeSqlIntegrationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SqlServer_MigrationsAndRowVersion_AreConfigured()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProyectoCafeDbContext>();

        Assert.True(await db.Database.CanConnectAsync());

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.NotEmpty(applied);

        Assert.NotNull(db.Model.FindEntityType(typeof(Venta)));
        Assert.NotNull(db.Model.FindEntityType(typeof(PagoVenta)));
        Assert.NotNull(db.Model.FindEntityType(typeof(LoteCafeExistencia)));

        var ventaRowVersion = db.Model.FindEntityType(typeof(Venta))!
            .FindProperty(nameof(Venta.RowVersion))!;
        var existenciaRowVersion = db.Model.FindEntityType(typeof(LoteCafeExistencia))!
            .FindProperty(nameof(LoteCafeExistencia.RowVersion))!;

        Assert.True(ventaRowVersion.IsConcurrencyToken);
        Assert.True(existenciaRowVersion.IsConcurrencyToken);

        Assert.True(await db.Ventas.CountAsync() >= 0);
        Assert.True(await db.PagosVenta.CountAsync() >= 0);
        Assert.True(await db.LotesCafeExistencias.CountAsync() >= 0);
    }
}