using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Infrastructure.Data;

/// <summary>
/// Contexto de datos del sistema. Persiste la seguridad (usuarios, roles y
/// permisos) y el inventario de café terminado por lotes.
/// </summary>
public sealed class ProyectoCafeDbContext(DbContextOptions<ProyectoCafeDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();

    public DbSet<PresentacionCafe> PresentacionesCafe => Set<PresentacionCafe>();
    public DbSet<TipoCafe> TiposCafe => Set<TipoCafe>();
    public DbSet<LoteCafeTerminado> LotesCafeTerminado => Set<LoteCafeTerminado>();
    public DbSet<LoteCafeExistencia> LotesCafeExistencias => Set<LoteCafeExistencia>();
    public DbSet<MovimientoLoteCafe> MovimientosLoteCafe => Set<MovimientoLoteCafe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", schema: "Seguridad");
            entity.HasKey(x => x.UsuarioId);

            entity.Property(x => x.UsuarioId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EmailNormalizado)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.EmailNormalizado)
                .IsUnique()
                .HasDatabaseName("UQ_Seguridad_Usuarios_EmailNormalizado");

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(UsuarioEstado.PendienteActivacion)
                .IsRequired();

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.IntentosFallidosLogin)
                .HasDefaultValue(0)
                .IsRequired();
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles", schema: "Seguridad");
            entity.HasKey(x => x.RolId);

            entity.Property(x => x.RolId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UQ_Seguridad_Roles_Nombre");

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasDefaultValue(RolEstado.Activo)
                .IsRequired();
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.ToTable("UsuariosRoles", schema: "Seguridad");
            entity.HasKey(x => new { x.UsuarioId, x.RolId });

            entity.HasOne(x => x.Usuario)
                .WithMany(u => u.UsuariosRoles)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Rol)
                .WithMany(r => r.UsuariosRoles)
                .HasForeignKey(x => x.RolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.ToTable("RolesPermisos", schema: "Seguridad");
            entity.HasKey(x => new { x.RolId, x.Modulo, x.Accion });

            entity.Property(x => x.Modulo)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(x => x.Accion)
                .HasMaxLength(12)
                .IsRequired();

            entity.HasOne(x => x.Rol)
                .WithMany(r => r.Permisos)
                .HasForeignKey(x => x.RolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PresentacionCafe>(entity =>
        {
            entity.ToTable("PresentacionesCafe", schema: "Inventario", table =>
            {
                table.HasCheckConstraint(
                    "CK_Inventario_PresentacionesCafe_PesoUnitarioGramos",
                    "[PesoUnitarioGramos] > 0");
            });
            entity.HasKey(x => x.PresentacionCafeId);

            entity.Property(x => x.PresentacionCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Codigo)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_PresentacionesCafe_Codigo");

            entity.Property(x => x.Nombre)
                .HasMaxLength(80)
                .IsRequired();

            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_PresentacionesCafe_Nombre");

            entity.Property(x => x.PesoUnitarioGramos)
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(EstadoCatalogoCafe.Activo)
                .IsRequired();

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.ActualizadoEn)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<TipoCafe>(entity =>
        {
            entity.ToTable("TiposCafe", schema: "Inventario");
            entity.HasKey(x => x.TipoCafeId);

            entity.Property(x => x.TipoCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Codigo)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_TiposCafe_Codigo");

            entity.Property(x => x.Nombre)
                .HasMaxLength(80)
                .IsRequired();

            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_TiposCafe_Nombre");

            entity.Property(x => x.Estado)
                .HasDefaultValue(EstadoCatalogoCafe.Activo)
                .IsRequired();

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.ActualizadoEn)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<LoteCafeTerminado>(entity =>
        {
            entity.ToTable("LotesCafeTerminado", schema: "Inventario", table =>
            {
                table.HasCheckConstraint(
                    "CK_Inventario_LotesCafeTerminado_Fechas",
                    "[FechaVencimiento] IS NULL OR [FechaVencimiento] >= [FechaProduccion]");
            });
            entity.HasKey(x => x.LoteCafeId);

            entity.Property(x => x.LoteCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CodigoLote)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.CodigoLote)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_LotesCafeTerminado_CodigoLote");

            entity.Property(x => x.FechaProduccion)
                .HasColumnType("datetime2(7)")
                .IsRequired();

            entity.Property(x => x.FechaEnvasado)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.FechaVencimiento)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.Estado)
                .HasDefaultValue(EstadoLoteCafe.Activo)
                .IsRequired();

            entity.Property(x => x.Observacion)
                .HasMaxLength(250);

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.ActualizadoEn)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasIndex(x => new { x.Estado, x.FechaProduccion })
                .HasDatabaseName("IX_Inventario_LotesCafeTerminado_Estado_FechaProduccion");
        });

        modelBuilder.Entity<LoteCafeExistencia>(entity =>
        {
            entity.ToTable("LotesCafeExistencias", schema: "Inventario", table =>
            {
                table.HasCheckConstraint(
                    "CK_Inventario_LotesCafeExistencias_CantidadPaquetes",
                    "[CantidadPaquetes] >= 0");
                table.HasCheckConstraint(
                    "CK_Inventario_LotesCafeExistencias_PesoUnitarioGramos",
                    "[PesoUnitarioGramos] > 0");
            });
            entity.HasKey(x => x.LoteCafeExistenciaId);

            entity.Property(x => x.LoteCafeExistenciaId)
                .ValueGeneratedOnAdd();

            entity.HasOne(x => x.LoteCafe)
                .WithMany(x => x.Existencias)
                .HasForeignKey(x => x.LoteCafeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PresentacionCafe)
                .WithMany()
                .HasForeignKey(x => x.PresentacionCafeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TipoCafe)
                .WithMany()
                .HasForeignKey(x => x.TipoCafeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.LoteCafeId, x.PresentacionCafeId, x.TipoCafeId })
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo");

            entity.HasIndex(x => new { x.PresentacionCafeId, x.TipoCafeId })
                .HasDatabaseName("IX_Inventario_LotesCafeExistencias_PresentacionTipo");

            entity.Property(x => x.CantidadPaquetes)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(x => x.PesoUnitarioGramos)
                .IsRequired();

            entity.Property(x => x.PesoNetoKg)
                .HasColumnType("decimal(18,3)")
                .HasComputedColumnSql(
                    "CAST((CAST([CantidadPaquetes] AS decimal(18,3)) * [PesoUnitarioGramos]) / 1000.0 AS decimal(18,3))",
                    stored: true)
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.ActualizadoEn)
                .HasColumnType("datetime2(7)");

            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<MovimientoLoteCafe>(entity =>
        {
            entity.ToTable("MovimientosLoteCafe", schema: "Inventario", table =>
            {
                table.HasCheckConstraint(
                    "CK_Inventario_MovimientosLoteCafe_CantidadPaquetes",
                    "[CantidadPaquetes] > 0");
                table.HasCheckConstraint(
                    "CK_Inventario_MovimientosLoteCafe_PesoNetoKg",
                    "[PesoNetoKg] > 0");
            });
            entity.HasKey(x => x.MovimientoLoteCafeId);

            entity.Property(x => x.MovimientoLoteCafeId)
                .ValueGeneratedOnAdd();

            entity.HasOne(x => x.LoteCafeExistencia)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.LoteCafeExistenciaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.TipoMovimiento)
                .IsRequired();

            entity.Property(x => x.CantidadPaquetes)
                .IsRequired();

            entity.Property(x => x.PesoNetoKg)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.FechaMovimiento)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.Referencia)
                .HasMaxLength(100);

            entity.Property(x => x.Observacion)
                .HasMaxLength(250);

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.CreadoEn)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.HasIndex(x => new { x.LoteCafeExistenciaId, x.FechaMovimiento })
                .HasDatabaseName("IX_Inventario_MovimientosLoteCafe_ExistenciaFecha");
        });
    }
}
