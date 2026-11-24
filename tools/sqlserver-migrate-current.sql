IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    IF SCHEMA_ID(N'Seguridad') IS NULL EXEC(N'CREATE SCHEMA [Seguridad];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE TABLE [Seguridad].[Roles] (
        [RolId] int NOT NULL IDENTITY,
        [Nombre] nvarchar(50) NOT NULL,
        [Descripcion] nvarchar(250) NULL,
        [Estado] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RolId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE TABLE [Seguridad].[Usuarios] (
        [UsuarioId] int NOT NULL IDENTITY,
        [Email] nvarchar(100) NOT NULL,
        [EmailNormalizado] nvarchar(100) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2 NULL,
        [UltimoLoginEn] datetime2 NULL,
        [IntentosFallidosLogin] int NOT NULL DEFAULT 0,
        [LockoutFin] datetime2 NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([UsuarioId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE TABLE [Seguridad].[RolesPermisos] (
        [RolId] int NOT NULL,
        [Modulo] nvarchar(120) NOT NULL,
        [Accion] nvarchar(12) NOT NULL,
        CONSTRAINT [PK_RolesPermisos] PRIMARY KEY ([RolId], [Modulo], [Accion]),
        CONSTRAINT [FK_RolesPermisos_Roles_RolId] FOREIGN KEY ([RolId]) REFERENCES [Seguridad].[Roles] ([RolId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE TABLE [Seguridad].[UsuariosRoles] (
        [UsuarioId] int NOT NULL,
        [RolId] int NOT NULL,
        CONSTRAINT [PK_UsuariosRoles] PRIMARY KEY ([UsuarioId], [RolId]),
        CONSTRAINT [FK_UsuariosRoles_Roles_RolId] FOREIGN KEY ([RolId]) REFERENCES [Seguridad].[Roles] ([RolId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsuariosRoles_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Seguridad].[Usuarios] ([UsuarioId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Seguridad_Roles_Nombre] ON [Seguridad].[Roles] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Seguridad_Usuarios_EmailNormalizado] ON [Seguridad].[Usuarios] ([EmailNormalizado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    CREATE INDEX [IX_UsuariosRoles_RolId] ON [Seguridad].[UsuariosRoles] ([RolId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023012804_InitialAdministration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023012804_InitialAdministration', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    IF SCHEMA_ID(N'Inventario') IS NULL EXEC(N'CREATE SCHEMA [Inventario];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE TABLE [Inventario].[LotesCafeTerminado] (
        [LoteCafeId] int NOT NULL IDENTITY,
        [CodigoLote] nvarchar(50) NOT NULL,
        [FechaProduccion] datetime2(7) NOT NULL,
        [FechaEnvasado] datetime2(7) NULL,
        [FechaVencimiento] datetime2(7) NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [Observacion] nvarchar(250) NULL,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LotesCafeTerminado] PRIMARY KEY ([LoteCafeId]),
        CONSTRAINT [CK_Inventario_LotesCafeTerminado_Fechas] CHECK ([FechaVencimiento] IS NULL OR [FechaVencimiento] >= [FechaProduccion])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE TABLE [Inventario].[PresentacionesCafe] (
        [PresentacionCafeId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [PesoUnitarioGramos] int NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PresentacionesCafe] PRIMARY KEY ([PresentacionCafeId]),
        CONSTRAINT [CK_Inventario_PresentacionesCafe_PesoUnitarioGramos] CHECK ([PesoUnitarioGramos] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE TABLE [Inventario].[TiposCafe] (
        [TipoCafeId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TiposCafe] PRIMARY KEY ([TipoCafeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE TABLE [Inventario].[LotesCafeExistencias] (
        [LoteCafeExistenciaId] int NOT NULL IDENTITY,
        [LoteCafeId] int NOT NULL,
        [PresentacionCafeId] int NOT NULL,
        [TipoCafeId] int NOT NULL,
        [CantidadPaquetes] int NOT NULL DEFAULT 0,
        [PesoUnitarioGramos] int NOT NULL,
        [PesoNetoKg] AS CAST((CAST([CantidadPaquetes] AS decimal(18,3)) * [PesoUnitarioGramos]) / 1000.0 AS decimal(18,3)) PERSISTED,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LotesCafeExistencias] PRIMARY KEY ([LoteCafeExistenciaId]),
        CONSTRAINT [CK_Inventario_LotesCafeExistencias_CantidadPaquetes] CHECK ([CantidadPaquetes] >= 0),
        CONSTRAINT [CK_Inventario_LotesCafeExistencias_PesoUnitarioGramos] CHECK ([PesoUnitarioGramos] > 0),
        CONSTRAINT [FK_LotesCafeExistencias_LotesCafeTerminado_LoteCafeId] FOREIGN KEY ([LoteCafeId]) REFERENCES [Inventario].[LotesCafeTerminado] ([LoteCafeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LotesCafeExistencias_PresentacionesCafe_PresentacionCafeId] FOREIGN KEY ([PresentacionCafeId]) REFERENCES [Inventario].[PresentacionesCafe] ([PresentacionCafeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LotesCafeExistencias_TiposCafe_TipoCafeId] FOREIGN KEY ([TipoCafeId]) REFERENCES [Inventario].[TiposCafe] ([TipoCafeId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE TABLE [Inventario].[MovimientosLoteCafe] (
        [MovimientoLoteCafeId] int NOT NULL IDENTITY,
        [LoteCafeExistenciaId] int NOT NULL,
        [TipoMovimiento] int NOT NULL,
        [CantidadPaquetes] int NOT NULL,
        [PesoNetoKg] decimal(18,3) NOT NULL,
        [FechaMovimiento] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [Referencia] nvarchar(100) NULL,
        [Observacion] nvarchar(250) NULL,
        [UsuarioId] int NULL,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        CONSTRAINT [PK_MovimientosLoteCafe] PRIMARY KEY ([MovimientoLoteCafeId]),
        CONSTRAINT [CK_Inventario_MovimientosLoteCafe_CantidadPaquetes] CHECK ([CantidadPaquetes] > 0),
        CONSTRAINT [CK_Inventario_MovimientosLoteCafe_PesoNetoKg] CHECK ([PesoNetoKg] > 0),
        CONSTRAINT [FK_MovimientosLoteCafe_LotesCafeExistencias_LoteCafeExistenciaId] FOREIGN KEY ([LoteCafeExistenciaId]) REFERENCES [Inventario].[LotesCafeExistencias] ([LoteCafeExistenciaId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovimientosLoteCafe_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Seguridad].[Usuarios] ([UsuarioId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE INDEX [IX_Inventario_LotesCafeExistencias_PresentacionTipo] ON [Inventario].[LotesCafeExistencias] ([PresentacionCafeId], [TipoCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE INDEX [IX_LotesCafeExistencias_TipoCafeId] ON [Inventario].[LotesCafeExistencias] ([TipoCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo] ON [Inventario].[LotesCafeExistencias] ([LoteCafeId], [PresentacionCafeId], [TipoCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE INDEX [IX_Inventario_LotesCafeTerminado_Estado_FechaProduccion] ON [Inventario].[LotesCafeTerminado] ([Estado], [FechaProduccion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_LotesCafeTerminado_CodigoLote] ON [Inventario].[LotesCafeTerminado] ([CodigoLote]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE INDEX [IX_Inventario_MovimientosLoteCafe_ExistenciaFecha] ON [Inventario].[MovimientosLoteCafe] ([LoteCafeExistenciaId], [FechaMovimiento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE INDEX [IX_MovimientosLoteCafe_UsuarioId] ON [Inventario].[MovimientosLoteCafe] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_PresentacionesCafe_Codigo] ON [Inventario].[PresentacionesCafe] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_PresentacionesCafe_Nombre] ON [Inventario].[PresentacionesCafe] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposCafe_Codigo] ON [Inventario].[TiposCafe] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposCafe_Nombre] ON [Inventario].[TiposCafe] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Inventario].[PresentacionesCafe] WHERE [Codigo] = N'250G')
    BEGIN
        INSERT INTO [Inventario].[PresentacionesCafe] ([Codigo], [Nombre], [PesoUnitarioGramos], [Estado])
        VALUES (N'250G', N'250 g', 250, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Inventario].[PresentacionesCafe] WHERE [Codigo] = N'454G')
    BEGIN
        INSERT INTO [Inventario].[PresentacionesCafe] ([Codigo], [Nombre], [PesoUnitarioGramos], [Estado])
        VALUES (N'454G', N'454 g', 454, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Inventario].[TiposCafe] WHERE [Codigo] = N'GRANO')
    BEGIN
        INSERT INTO [Inventario].[TiposCafe] ([Codigo], [Nombre], [Estado])
        VALUES (N'GRANO', N'Grano', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Inventario].[TiposCafe] WHERE [Codigo] = N'MOLIDO')
    BEGIN
        INSERT INTO [Inventario].[TiposCafe] ([Codigo], [Nombre], [Estado])
        VALUES (N'MOLIDO', N'Molido', 1);
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028025431_AddInventarioCafeTerminado'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028025431_AddInventarioCafeTerminado', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030233633_AddVentasPrecios'
)
BEGIN
    IF SCHEMA_ID(N'Ventas') IS NULL EXEC(N'CREATE SCHEMA [Ventas];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030233633_AddVentasPrecios'
)
BEGIN
    CREATE TABLE [Ventas].[PreciosVenta] (
        [PrecioVentaId] int NOT NULL IDENTITY,
        [Nombre] nvarchar(80) NOT NULL,
        [PresentacionCafeId] int NOT NULL,
        [Precio] decimal(18,2) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PreciosVenta] PRIMARY KEY ([PrecioVentaId]),
        CONSTRAINT [FK_PreciosVenta_PresentacionesCafe_PresentacionCafeId] FOREIGN KEY ([PresentacionCafeId]) REFERENCES [Inventario].[PresentacionesCafe] ([PresentacionCafeId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030233633_AddVentasPrecios'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Ventas_PreciosVenta_PresentacionNombre] ON [Ventas].[PreciosVenta] ([PresentacionCafeId], [Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030233633_AddVentasPrecios'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030233633_AddVentasPrecios', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030234522_AddVentasClientes'
)
BEGIN
    CREATE TABLE [Ventas].[Clientes] (
        [ClienteId] int NOT NULL IDENTITY,
        [TipoIdentificacion] nvarchar(10) NOT NULL,
        [NumeroIdentificacion] nvarchar(30) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Telefono] nvarchar(30) NULL,
        [Email] nvarchar(100) NULL,
        [Direccion] nvarchar(200) NULL,
        [Observacion] nvarchar(250) NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([ClienteId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030234522_AddVentasClientes'
)
BEGIN
    CREATE INDEX [IX_Ventas_Clientes_Nombre] ON [Ventas].[Clientes] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030234522_AddVentasClientes'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Ventas_Clientes_NumeroIdentificacion] ON [Ventas].[Clientes] ([NumeroIdentificacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030234522_AddVentasClientes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030234522_AddVentasClientes', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE TABLE [Ventas].[Ventas] (
        [VentaId] int NOT NULL IDENTITY,
        [NumeroVenta] int NOT NULL,
        [FechaVenta] datetime2(7) NOT NULL,
        [ClienteId] int NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [Observacion] nvarchar(250) NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Ventas] PRIMARY KEY ([VentaId]),
        CONSTRAINT [FK_Ventas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Ventas].[Clientes] ([ClienteId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE TABLE [Ventas].[VentaDetalles] (
        [VentaDetalleId] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [PresentacionCafeId] int NOT NULL,
        [TipoCafeId] int NOT NULL,
        [PrecioVentaId] int NOT NULL,
        [CantidadPaquetes] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        CONSTRAINT [PK_VentaDetalles] PRIMARY KEY ([VentaDetalleId]),
        CONSTRAINT [FK_VentaDetalles_PreciosVenta_PrecioVentaId] FOREIGN KEY ([PrecioVentaId]) REFERENCES [Ventas].[PreciosVenta] ([PrecioVentaId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VentaDetalles_PresentacionesCafe_PresentacionCafeId] FOREIGN KEY ([PresentacionCafeId]) REFERENCES [Inventario].[PresentacionesCafe] ([PresentacionCafeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VentaDetalles_TiposCafe_TipoCafeId] FOREIGN KEY ([TipoCafeId]) REFERENCES [Inventario].[TiposCafe] ([TipoCafeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VentaDetalles_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas].[Ventas] ([VentaId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_VentaDetalles_PrecioVentaId] ON [Ventas].[VentaDetalles] ([PrecioVentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_VentaDetalles_PresentacionCafeId] ON [Ventas].[VentaDetalles] ([PresentacionCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_VentaDetalles_TipoCafeId] ON [Ventas].[VentaDetalles] ([TipoCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_VentaDetalles_VentaId] ON [Ventas].[VentaDetalles] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_Ventas_ClienteId] ON [Ventas].[Ventas] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE INDEX [IX_Ventas_Ventas_FechaVenta] ON [Ventas].[Ventas] ([FechaVenta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Ventas_Ventas_NumeroVenta] ON [Ventas].[Ventas] ([NumeroVenta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030235506_AddVentas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030235506_AddVentas', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101003126_AddVariedadesCafe'
)
BEGIN
    CREATE TABLE [Inventario].[VariedadesCafe] (
        [VariedadCafeId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VariedadesCafe] PRIMARY KEY ([VariedadCafeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101003126_AddVariedadesCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_VariedadesCafe_Codigo] ON [Inventario].[VariedadesCafe] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101003126_AddVariedadesCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_VariedadesCafe_Nombre] ON [Inventario].[VariedadesCafe] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101003126_AddVariedadesCafe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101003126_AddVariedadesCafe', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004027_AddTiposTuesteCafe'
)
BEGIN
    CREATE TABLE [Inventario].[TiposTuesteCafe] (
        [TipoTuesteCafeId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TiposTuesteCafe] PRIMARY KEY ([TipoTuesteCafeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004027_AddTiposTuesteCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposTuesteCafe_Codigo] ON [Inventario].[TiposTuesteCafe] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004027_AddTiposTuesteCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposTuesteCafe_Nombre] ON [Inventario].[TiposTuesteCafe] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004027_AddTiposTuesteCafe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101004027_AddTiposTuesteCafe', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004801_AddTiposProcesoCafe'
)
BEGIN
    CREATE TABLE [Inventario].[TiposProcesoCafe] (
        [TipoProcesoCafeId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [Estado] int NOT NULL DEFAULT 1,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        [ActualizadoEn] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TiposProcesoCafe] PRIMARY KEY ([TipoProcesoCafeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004801_AddTiposProcesoCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposProcesoCafe_Codigo] ON [Inventario].[TiposProcesoCafe] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004801_AddTiposProcesoCafe'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Inventario_TiposProcesoCafe_Nombre] ON [Inventario].[TiposProcesoCafe] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101004801_AddTiposProcesoCafe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101004801_AddTiposProcesoCafe', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD [TipoProcesoCafeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD [TipoTuesteCafeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD [VariedadCafeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    CREATE INDEX [IX_Inventario_LotesCafeTerminado_TipoProcesoCafeId] ON [Inventario].[LotesCafeTerminado] ([TipoProcesoCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    CREATE INDEX [IX_Inventario_LotesCafeTerminado_TipoTuesteCafeId] ON [Inventario].[LotesCafeTerminado] ([TipoTuesteCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    CREATE INDEX [IX_Inventario_LotesCafeTerminado_VariedadCafeId] ON [Inventario].[LotesCafeTerminado] ([VariedadCafeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD CONSTRAINT [FK_LotesCafeTerminado_TiposProcesoCafe_TipoProcesoCafeId] FOREIGN KEY ([TipoProcesoCafeId]) REFERENCES [Inventario].[TiposProcesoCafe] ([TipoProcesoCafeId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD CONSTRAINT [FK_LotesCafeTerminado_TiposTuesteCafe_TipoTuesteCafeId] FOREIGN KEY ([TipoTuesteCafeId]) REFERENCES [Inventario].[TiposTuesteCafe] ([TipoTuesteCafeId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    ALTER TABLE [Inventario].[LotesCafeTerminado] ADD CONSTRAINT [FK_LotesCafeTerminado_VariedadesCafe_VariedadCafeId] FOREIGN KEY ([VariedadCafeId]) REFERENCES [Inventario].[VariedadesCafe] ([VariedadCafeId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101005741_AddLoteCatalogos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101005741_AddLoteCatalogos', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101015121_AddPagosYEstadosEntrega'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [EstadoEntrega] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101015121_AddPagosYEstadosEntrega'
)
BEGIN
    CREATE TABLE [Ventas].[PagosVenta] (
        [PagoVentaId] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        [FechaPago] datetime2(7) NOT NULL,
        [MetodoPago] nvarchar(50) NOT NULL,
        [Referencia] nvarchar(100) NULL,
        [UsuarioId] int NULL,
        [CreadoEn] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
        CONSTRAINT [PK_PagosVenta] PRIMARY KEY ([PagoVentaId]),
        CONSTRAINT [CK_Ventas_PagosVenta_Monto] CHECK ([Monto] > 0),
        CONSTRAINT [FK_PagosVenta_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas].[Ventas] ([VentaId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101015121_AddPagosYEstadosEntrega'
)
BEGIN
    CREATE INDEX [IX_Ventas_PagosVenta_VentaId] ON [Ventas].[PagosVenta] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101015121_AddPagosYEstadosEntrega'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101015121_AddPagosYEstadosEntrega', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [AnuladoEn] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [AnuladoPorUsuarioId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [CanceladoEn] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [CanceladoPorUsuarioId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [MotivoAnulacion] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD [MotivoCancelacion] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    CREATE INDEX [IX_Ventas_AnuladoPorUsuarioId] ON [Ventas].[Ventas] ([AnuladoPorUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    CREATE INDEX [IX_Ventas_CanceladoPorUsuarioId] ON [Ventas].[Ventas] ([CanceladoPorUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD CONSTRAINT [FK_Ventas_Usuarios_AnuladoPorUsuarioId] FOREIGN KEY ([AnuladoPorUsuarioId]) REFERENCES [Seguridad].[Usuarios] ([UsuarioId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    ALTER TABLE [Ventas].[Ventas] ADD CONSTRAINT [FK_Ventas_Usuarios_CanceladoPorUsuarioId] FOREIGN KEY ([CanceladoPorUsuarioId]) REFERENCES [Seguridad].[Usuarios] ([UsuarioId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101021158_AddTrazabilidadCancelacionAnulacion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101021158_AddTrazabilidadCancelacionAnulacion', N'10.0.15');
END;

COMMIT;
GO

