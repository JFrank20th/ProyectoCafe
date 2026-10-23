using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Infrastructure.Data;

/// <summary>
/// Contexto de datos del sistema. El alcance actual del proyecto se limita a los
/// módulos de Administración y Dashboard, por lo que solo persiste seguridad
/// (usuarios, roles y permisos).
/// </summary>
public sealed class ProyectoCafeDbContext(DbContextOptions<ProyectoCafeDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();

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
    }
}
