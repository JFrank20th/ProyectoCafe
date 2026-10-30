using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.Tests;

public sealed class InventarioCafeModelTests
{
    private static ProyectoCafeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProyectoCafeDbContext>()
            .UseInMemoryDatabase($"inventario-cafe-tests-{Guid.NewGuid():N}")
            .Options;

        return new ProyectoCafeDbContext(options);
    }

    [Fact]
    public void Model_MapsInventarioCafeTablesToInventarioSchema()
    {
        using var db = CreateContext();
        var model = db.Model;

        AssertTable(model, typeof(PresentacionCafe), "PresentacionesCafe");
        AssertTable(model, typeof(TipoCafe), "TiposCafe");
        AssertTable(model, typeof(LoteCafeTerminado), "LotesCafeTerminado");
        AssertTable(model, typeof(LoteCafeExistencia), "LotesCafeExistencias");
        AssertTable(model, typeof(MovimientoLoteCafe), "MovimientosLoteCafe");
    }

    [Fact]
    public void Existencia_HasUniqueCombinationPerLotePresentacionTipo()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(LoteCafeExistencia));

        Assert.NotNull(entity);

        var index = Assert.Single(
            entity.GetIndexes(),
            x => x.GetDatabaseName() == "UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo");

        Assert.True(index.IsUnique);
        Assert.Equal(
            ["LoteCafeId", "PresentacionCafeId", "TipoCafeId"],
            index.Properties.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Existencia_ComputesPesoNetoKgInDatabase()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(LoteCafeExistencia));

        Assert.NotNull(entity);

        var property = entity.FindProperty(nameof(LoteCafeExistencia.PesoNetoKg));

        Assert.NotNull(property);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Contains("PesoUnitarioGramos", property.GetComputedColumnSql());
    }

    [Fact]
    public void Model_DeclaresCheckConstraintsAndRestrictDeletes()
    {
        using var db = CreateContext();
        var model = db.GetService<IDesignTimeModel>().Model;

        var existencia = model.FindEntityType(typeof(LoteCafeExistencia));
        var movimiento = model.FindEntityType(typeof(MovimientoLoteCafe));

        Assert.NotNull(existencia);
        Assert.NotNull(movimiento);

        Assert.Contains(
            existencia.GetCheckConstraints(),
            x => x.Name == "CK_Inventario_LotesCafeExistencias_CantidadPaquetes");
        Assert.Contains(
            existencia.GetCheckConstraints(),
            x => x.Name == "CK_Inventario_LotesCafeExistencias_PesoUnitarioGramos");
        Assert.Contains(
            movimiento.GetCheckConstraints(),
            x => x.Name == "CK_Inventario_MovimientosLoteCafe_CantidadPaquetes");
        Assert.Contains(
            movimiento.GetCheckConstraints(),
            x => x.Name == "CK_Inventario_MovimientosLoteCafe_PesoNetoKg");

        Assert.All(
            existencia.GetForeignKeys(),
            x => Assert.Equal(DeleteBehavior.Restrict, x.DeleteBehavior));
        Assert.All(
            movimiento.GetForeignKeys(),
            x => Assert.Equal(DeleteBehavior.Restrict, x.DeleteBehavior));
    }

    [Fact]
    public async Task Lote_StoresIndependentQuantitiesPerPresentationAndType()
    {
        await using var db = CreateContext();

        var presentacion250 = new PresentacionCafe
        {
            Codigo = "250G",
            Nombre = "250 g",
            PesoUnitarioGramos = 250,
        };
        var presentacion454 = new PresentacionCafe
        {
            Codigo = "454G",
            Nombre = "454 g",
            PesoUnitarioGramos = 454,
        };
        var grano = new TipoCafe { Codigo = "GRANO", Nombre = "Grano" };
        var molido = new TipoCafe { Codigo = "MOLIDO", Nombre = "Molido" };

        var lote = new LoteCafeTerminado
        {
            CodigoLote = "LC-2026-001",
            FechaProduccion = new DateTime(2026, 10, 1),
        };

        lote.Existencias.Add(CreateExistencia(presentacion250, grano, 120));
        lote.Existencias.Add(CreateExistencia(presentacion250, molido, 80));
        lote.Existencias.Add(CreateExistencia(presentacion454, grano, 60));
        lote.Existencias.Add(CreateExistencia(presentacion454, molido, 40));

        db.LotesCafeTerminado.Add(lote);
        await db.SaveChangesAsync();

        var saved = await db.LotesCafeTerminado
            .Include(x => x.Existencias)
            .SingleAsync();

        Assert.Equal(4, saved.Existencias.Count);
        Assert.Equal(
            120,
            saved.Existencias.Single(x =>
                x.PresentacionCafeId == presentacion250.PresentacionCafeId &&
                x.TipoCafeId == grano.TipoCafeId).CantidadPaquetes);
        Assert.Equal(
            80,
            saved.Existencias.Single(x =>
                x.PresentacionCafeId == presentacion250.PresentacionCafeId &&
                x.TipoCafeId == molido.TipoCafeId).CantidadPaquetes);
        Assert.Equal(
            60,
            saved.Existencias.Single(x =>
                x.PresentacionCafeId == presentacion454.PresentacionCafeId &&
                x.TipoCafeId == grano.TipoCafeId).CantidadPaquetes);
        Assert.Equal(
            40,
            saved.Existencias.Single(x =>
                x.PresentacionCafeId == presentacion454.PresentacionCafeId &&
                x.TipoCafeId == molido.TipoCafeId).CantidadPaquetes);
    }

    private static LoteCafeExistencia CreateExistencia(
        PresentacionCafe presentacion,
        TipoCafe tipo,
        int cantidadPaquetes) =>
        new()
        {
            PresentacionCafe = presentacion,
            TipoCafe = tipo,
            CantidadPaquetes = cantidadPaquetes,
            PesoUnitarioGramos = presentacion.PesoUnitarioGramos,
        };

    private static void AssertTable(
        Microsoft.EntityFrameworkCore.Metadata.IModel model,
        Type entityType,
        string tableName)
    {
        var entity = model.FindEntityType(entityType);

        Assert.NotNull(entity);
        Assert.Equal("Inventario", entity.GetSchema());
        Assert.Equal(tableName, entity.GetTableName());
    }
}
