using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Infrastructure.Data;

public sealed class ProyectoCafeDbContext(DbContextOptions<ProyectoCafeDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Presentacion> Presentaciones => Set<Presentacion>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<LoteMovimiento> LoteMovimientos => Set<LoteMovimiento>();
    public DbSet<Finca> Fincas => Set<Finca>();
    public DbSet<Parcela> Parcelas => Set<Parcela>();
    public DbSet<OperacionAgricola> OperacionesAgricolas => Set<OperacionAgricola>();
    public DbSet<CosechaCafe> CosechasCafe => Set<CosechaCafe>();
    public DbSet<TransformacionCafe> TransformacionesCafe => Set<TransformacionCafe>();
    public DbSet<EmpaqueCafe> EmpaquesCafe => Set<EmpaqueCafe>();
    public DbSet<EmpaqueCafeDetalle> EmpaquesCafeDetalles => Set<EmpaqueCafeDetalle>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<RecepcionCafe> RecepcionesCafe => Set<RecepcionCafe>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<TipoVenta> TiposVenta => Set<TipoVenta>();
    public DbSet<ProductoPrecioTipoVenta> ProductosPreciosTipoVenta => Set<ProductoPrecioTipoVenta>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<VentaPago> VentaPagos => Set<VentaPago>();
    public DbSet<Costo> Costos => Set<Costo>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<Pasivo> Pasivos => Set<Pasivo>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias", schema: "Inventario");
            entity.HasKey(x => x.CategoriaId);

            entity.Property(x => x.CategoriaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<UnidadMedida>(entity =>
        {
            entity.ToTable("UnidadesMedida", schema: "Inventario");
            entity.HasKey(x => x.Codigo);

            entity.Property(x => x.Codigo)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Nombre)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasData(
                new UnidadMedida { Codigo = "UN", Nombre = "Unidad", Estado = true },
                new UnidadMedida { Codigo = "G", Nombre = "Gramo", Estado = true },
                new UnidadMedida { Codigo = "LB", Nombre = "Libra", Estado = true },
                new UnidadMedida { Codigo = "KG", Nombre = "Kilogramo", Estado = true },
                new UnidadMedida { Codigo = "L", Nombre = "Litro", Estado = true }
            );
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos", schema: "Inventario");
            entity.HasKey(x => x.ProductoId);

            entity.Property(x => x.ProductoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Precio)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.PrecioInternet)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.PrecioMayorista)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Stock)
                .HasColumnType("decimal(18,3)")
                .HasDefaultValue(0m);

            entity.Property(x => x.ManejaLotes)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(x => x.TipoLote)
                .HasMaxLength(30)
                .HasDefaultValue("Manual")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.Property(x => x.UnidadMedida)
                .HasMaxLength(20)
                .HasDefaultValue("UN")
                .IsRequired();

            entity.Property(x => x.PermiteDecimal)
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasOne<Categoria>()
                .WithMany()
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.PresentacionId)
                .HasColumnName("PresentacionCafeId");

            entity.HasIndex(x => x.PresentacionId)
                .HasDatabaseName("IX_Productos_PresentacionCafeId");

            entity.HasOne<Presentacion>()
                .WithMany()
                .HasForeignKey(x => x.PresentacionId)
                .HasConstraintName("FK_Productos_PresentacionesCafe_PresentacionCafeId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UnidadMedida>()
                .WithMany()
                .HasForeignKey(x => x.UnidadMedida)
                .HasPrincipalKey(x => x.Codigo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Presentacion>(entity =>
        {
            entity.ToTable("PresentacionesCafe", schema: "Inventario");
            entity.HasKey(x => x.PresentacionId);

            entity.Property(x => x.PresentacionId)
                .HasColumnName("PresentacionCafeId")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Codigo)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("UQ_Inventario_PresentacionesCafe_Codigo");

            entity.Property(x => x.Nombre)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PesoGramosLegacy)
                .HasColumnName("PesoGramos")
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.UnidadMedida)
                .HasMaxLength(20)
                .HasDefaultValue("G")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasOne<UnidadMedida>()
                .WithMany()
                .HasForeignKey(x => x.UnidadMedida)
                .HasPrincipalKey(x => x.Codigo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.ToTable("Insumos", schema: "Inventario");
            entity.HasKey(x => x.InsumoId);

            entity.Property(x => x.InsumoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .HasDefaultValue(0m);

            entity.Property(x => x.CostoUnitario)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m);

            entity.Property(x => x.UnidadMedida)
                .HasMaxLength(20)
                .HasDefaultValue("UN")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasOne<UnidadMedida>()
                .WithMany()
                .HasForeignKey(x => x.UnidadMedida)
                .HasPrincipalKey(x => x.Codigo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.ToTable("Lotes", schema: "Inventario");
            entity.HasKey(x => x.LoteId);

            entity.Property(x => x.LoteId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CodigoLote)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.CodigoLote)
                .IsUnique()
                .HasDatabaseName("UX_Inventario_Lotes_CodigoLote");

            entity.Property(x => x.CantidadInicial)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CantidadDisponible)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Activo")
                .IsRequired();

            entity.Property(x => x.FechaCreacion)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.HasIndex(x => new { x.ProductoId, x.Estado, x.FechaCreacion, x.LoteId })
                .HasDatabaseName("IX_Inventario_Lotes_Producto");

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LoteMovimiento>(entity =>
        {
            entity.ToTable("LoteMovimientos", schema: "Inventario");
            entity.HasKey(x => x.MovimientoId);

            entity.Property(x => x.MovimientoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Tipo)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Fecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.Observacion)
                .HasMaxLength(250);

            entity.HasIndex(x => new { x.LoteId, x.MovimientoId })
                .HasDatabaseName("IX_Inventario_LoteMovimientos_Lote");

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Finca>(entity =>
        {
            entity.ToTable("Fincas", schema: "Produccion");
            entity.HasKey(x => x.FincaId);

            entity.Property(x => x.FincaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Ubicacion)
                .HasMaxLength(200);

            entity.Property(x => x.Municipio)
                .HasMaxLength(100);

            entity.Property(x => x.Departamento)
                .HasMaxLength(100);

            entity.Property(x => x.AreaHectareas)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Parcela>(entity =>
        {
            entity.ToTable("Parcelas", schema: "Produccion");
            entity.HasKey(x => x.ParcelaId);

            entity.Property(x => x.ParcelaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Descripcion)
                .HasMaxLength(250);

            entity.Property(x => x.Cultivo)
                .HasColumnName("VariedadCafe")
                .HasMaxLength(100);

            entity.Property(x => x.AreaHectareas)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.FincaId, x.Nombre })
                .HasDatabaseName("UX_Produccion_Parcelas_Finca_Nombre")
                .IsUnique();

            entity.HasOne<Finca>()
                .WithMany()
                .HasForeignKey(x => x.FincaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoCosechaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperacionAgricola>(entity =>
        {
            entity.ToTable("OperacionesAgricolas", schema: "Produccion");
            entity.HasKey(x => x.OperacionAgricolaId);

            entity.Property(x => x.OperacionAgricolaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TipoOperacion)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.FechaOperacion)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)");

            entity.Property(x => x.CantidadInsumo)
                .HasColumnType("decimal(18,3)");

            entity.Property(x => x.Cultivo)
                .HasMaxLength(30);

            entity.Property(x => x.Humedad)
                .HasColumnType("decimal(5,2)");

            entity.Property(x => x.CostoInsumo)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.CostoManoObra)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.CostoOtros)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.CostoTotal)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Observaciones)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrada")
                .IsRequired();

            entity.HasIndex(x => new { x.ParcelaId, x.FechaOperacion })
                .HasDatabaseName("IX_Produccion_OperacionesAgricolas_Parcela_Fecha");

            entity.HasIndex(x => new { x.FechaOperacion, x.Estado })
                .HasDatabaseName("IX_Produccion_OperacionesAgricolas_Fecha_Estado");

            entity.HasOne<Parcela>()
                .WithMany()
                .HasForeignKey(x => x.ParcelaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Insumo>()
                .WithMany()
                .HasForeignKey(x => x.InsumoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CosechaCafe>(entity =>
        {
            entity.ToTable("CosechasCafe", schema: "Produccion");
            entity.HasKey(x => x.CosechaCafeId);

            entity.Property(x => x.CosechaCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Cultivo)
                .HasColumnName("TipoCafe")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.FechaCosecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.Humedad)
                .HasColumnType("decimal(5,2)");

            entity.Property(x => x.Observaciones)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrada")
                .IsRequired();

            entity.HasIndex(x => new { x.ParcelaId, x.FechaCosecha })
                .HasDatabaseName("IX_Produccion_CosechasCafe_Parcela_Fecha");

            entity.HasIndex(x => new { x.FechaCosecha, x.Estado })
                .HasDatabaseName("IX_Produccion_CosechasCafe_Fecha_Estado");

            entity.HasOne<Parcela>()
                .WithMany()
                .HasForeignKey(x => x.ParcelaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransformacionCafe>(entity =>
        {
            entity.ToTable("TransformacionesCafe", schema: "Produccion");
            entity.HasKey(x => x.TransformacionCafeId);

            entity.Property(x => x.TransformacionCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TipoTransformacion)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.FechaTransformacion)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CantidadEntrada)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CantidadSalida)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CostoProceso)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Merma)
                .HasColumnType("decimal(18,3)");

            entity.Property(x => x.Rendimiento)
                .HasColumnType("decimal(9,4)");

            entity.Property(x => x.Observaciones)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrada")
                .IsRequired();

            entity.HasIndex(x => new { x.FechaTransformacion, x.Estado })
                .HasDatabaseName("IX_Produccion_TransformacionesCafe_Fecha_Estado");

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoSalidaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteSalidaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmpaqueCafe>(entity =>
        {
            entity.ToTable("EmpaquesCafe", schema: "Produccion");
            entity.HasKey(x => x.EmpaqueCafeId);

            entity.Property(x => x.EmpaqueCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.FechaEmpaque)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CantidadOrigenConsumida)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CantidadEmpaques)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PesoTotalGramos)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CostoEmpaque)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Observaciones)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrado")
                .IsRequired();

            entity.HasIndex(x => new { x.FechaEmpaque, x.Estado })
                .HasDatabaseName("IX_Produccion_EmpaquesCafe_Fecha_Estado");

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteOrigenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmpaqueCafeDetalle>(entity =>
        {
            entity.ToTable("EmpaquesCafeDetalles", schema: "Produccion");
            entity.HasKey(x => x.EmpaqueCafeDetalleId);

            entity.Property(x => x.EmpaqueCafeDetalleId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PesoGramosUnitario)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PesoTotalGramos)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CantidadOrigenAsignada)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.CostoOrigenAsignado)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.CostoEmpaqueAsignado)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrado")
                .IsRequired();

            entity.HasIndex(x => x.EmpaqueCafeId)
                .HasDatabaseName("IX_Produccion_EmpaquesCafeDetalles_EmpaqueCafe");

            entity.HasIndex(x => x.LoteSalidaId)
                .IsUnique()
                .HasDatabaseName("UQ_Produccion_EmpaquesCafeDetalles_LoteSalida");

            entity.HasOne<EmpaqueCafe>()
                .WithMany()
                .HasForeignKey(x => x.EmpaqueCafeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoSalidaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.PresentacionId)
                .HasColumnName("PresentacionCafeId");

            entity.HasIndex(x => x.PresentacionId)
                .HasDatabaseName("IX_EmpaquesCafeDetalles_PresentacionCafeId");

            entity.HasOne<Presentacion>()
                .WithMany()
                .HasForeignKey(x => x.PresentacionId)
                .HasConstraintName("FK_EmpaquesCafeDetalles_PresentacionesCafe_PresentacionCafeId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteSalidaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("Proveedores", schema: "Compras");
            entity.HasKey(x => x.ProveedorId);

            entity.Property(x => x.ProveedorId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Documento)
                .HasMaxLength(50);

            entity.Property(x => x.Telefono)
                .HasMaxLength(50);

            entity.Property(x => x.Email)
                .HasMaxLength(100);

            entity.Property(x => x.Direccion)
                .HasMaxLength(200);

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<RecepcionCafe>(entity =>
        {
            entity.ToTable("RecepcionesCafe", schema: "Compras");
            entity.HasKey(x => x.RecepcionCafeId);

            entity.Property(x => x.RecepcionCafeId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TipoCafe)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.FechaRecepcion)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.NumeroDocumento)
                .HasMaxLength(50);

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PrecioUnitario)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Total)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Humedad)
                .HasColumnType("decimal(5,2)");

            entity.Property(x => x.Observaciones)
                .HasMaxLength(250);

            entity.Property(x => x.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Registrada")
                .IsRequired();

            entity.HasIndex(x => new { x.ProveedorId, x.NumeroDocumento })
                .HasDatabaseName("UX_Compras_RecepcionesCafe_Proveedor_Documento")
                .IsUnique()
                .HasFilter("[NumeroDocumento] IS NOT NULL");

            entity.HasIndex(x => new { x.FechaRecepcion, x.Estado })
                .HasDatabaseName("IX_Compras_RecepcionesCafe_Fecha_Estado");

            entity.HasOne<Proveedor>()
                .WithMany()
                .HasForeignKey(x => x.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes", schema: "Negocio");
            entity.HasKey(x => x.ClienteId);

            entity.Property(x => x.ClienteId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Documento)
                .HasMaxLength(50);

            entity.HasIndex(x => x.Documento)
                .IsUnique()
                .HasDatabaseName("UQ_Negocio_Clientes_Documento");

            entity.Property(x => x.Telefono)
                .HasMaxLength(50);

            entity.Property(x => x.Email)
                .HasMaxLength(100);

            entity.Property(x => x.Direccion)
                .HasMaxLength(200);

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.Property(x => x.FechaCreacion)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<TipoVenta>(entity =>
        {
            entity.ToTable("TiposVenta", schema: "Negocio");
            entity.HasKey(x => x.TipoVentaId);

            entity.Property(x => x.TipoVentaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Codigo)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("UQ_Negocio_TiposVenta_Codigo");

            entity.Property(x => x.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Orden)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasData(
                new TipoVenta { TipoVentaId = 1, Codigo = TiposVentaPredefinidos.Normal, Nombre = "Precio normal", Orden = 10, Estado = true },
                new TipoVenta { TipoVentaId = 2, Codigo = TiposVentaPredefinidos.Internet, Nombre = "Precio internet", Orden = 20, Estado = true },
                new TipoVenta { TipoVentaId = 3, Codigo = TiposVentaPredefinidos.Mayorista, Nombre = "Precio mayorista", Orden = 30, Estado = true }
            );
        });

        modelBuilder.Entity<ProductoPrecioTipoVenta>(entity =>
        {
            entity.ToTable("ProductosPreciosTipoVenta", schema: "Negocio");
            entity.HasKey(x => x.ProductoPrecioTipoVentaId);

            entity.Property(x => x.ProductoPrecioTipoVentaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Precio)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.ProductoId, x.TipoVentaId })
                .IsUnique()
                .HasDatabaseName("UQ_Negocio_ProductosPreciosTipoVenta_Producto_TipoVenta");

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TipoVenta>()
                .WithMany()
                .HasForeignKey(x => x.TipoVentaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("Ventas", schema: "Negocio");
            entity.HasKey(x => x.VentaId);

            entity.Property(x => x.VentaId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Fecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Total)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.EstadoPago)
                .HasMaxLength(20)
                .HasDefaultValue("Pagado")
                .IsRequired();

            entity.Property(x => x.TipoPrecio)
                .HasMaxLength(30)
                .HasDefaultValue("Normal")
                .IsRequired();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.ToTable("VentaDetalles", schema: "Negocio");
            entity.HasKey(x => x.DetalleId);

            entity.Property(x => x.DetalleId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Cantidad)
                .HasColumnType("decimal(18,3)")
                .IsRequired();

            entity.Property(x => x.PrecioUnitario)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.CostoUnitario)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(x => x.Subtotal)
                .HasColumnType("decimal(18,2)")
                .HasComputedColumnSql("([Cantidad]*[PrecioUnitario])", stored: true);

            entity.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(x => x.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.ToTable("MetodosPago", schema: "Negocio");
            entity.HasKey(x => x.MetodoPagoId);

            entity.Property(x => x.MetodoPagoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(80)
                .IsRequired();

            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UQ_Negocio_MetodosPago_Nombre");

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasData(
                new MetodoPago { MetodoPagoId = 1, Nombre = "Efectivo", Estado = true },
                new MetodoPago { MetodoPagoId = 2, Nombre = "Transferencia", Estado = true },
                new MetodoPago { MetodoPagoId = 3, Nombre = "Tarjeta", Estado = true }
            );
        });

        modelBuilder.Entity<VentaPago>(entity =>
        {
            entity.ToTable("VentaPagos", schema: "Negocio");
            entity.HasKey(x => x.VentaPagoId);

            entity.Property(x => x.VentaPagoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.HasOne<Venta>()
                .WithMany()
                .HasForeignKey(x => x.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<MetodoPago>()
                .WithMany()
                .HasForeignKey(x => x.MetodoPagoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Costo>(entity =>
        {
            entity.ToTable("Costos", schema: "Contabilidad");
            entity.HasKey(x => x.CostoId);

            entity.Property(x => x.CostoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.TipoCosto)
                .HasMaxLength(30)
                .HasDefaultValue("Manual")
                .IsRequired();

            entity.Property(x => x.FuenteModulo)
                .HasMaxLength(120);

            entity.HasIndex(x => new { x.FuenteModulo, x.ReferenciaId })
                .HasDatabaseName("IX_Contabilidad_Costos_Fuente_Referencia");

            entity.HasIndex(x => new { x.ParcelaId, x.Fecha })
                .HasDatabaseName("IX_Contabilidad_Costos_Parcela_Fecha");

            entity.HasIndex(x => new { x.LoteId, x.Fecha })
                .HasDatabaseName("IX_Contabilidad_Costos_Lote_Fecha");

            entity.Property(x => x.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Fecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);

            entity.HasOne<Parcela>()
                .WithMany()
                .HasForeignKey(x => x.ParcelaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Lote>()
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.ToTable("Gastos", schema: "Contabilidad");
            entity.HasKey(x => x.GastoId);

            entity.Property(x => x.GastoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Fecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Pasivo>(entity =>
        {
            entity.ToTable("Pasivos", schema: "Contabilidad");
            entity.HasKey(x => x.PasivoId);

            entity.Property(x => x.PasivoId)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(x => x.Fecha)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("sysdatetime()")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Estado)
                .HasDefaultValue(true);
        });

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
