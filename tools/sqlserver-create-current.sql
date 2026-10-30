/*
  Creacion de la base de datos vigente de ProyectoCafe.

  Alcance actual del proyecto:
  - Administracion (Usuarios, Roles, UsuariosRoles, RolesPermisos)
  - Inventario de cafe terminado (PresentacionesCafe, TiposCafe,
    LotesCafeTerminado, LotesCafeExistencias, MovimientosLoteCafe)
  - Historial de EF Core alineado a las migraciones vigentes:
    20261023012804_InitialAdministration
    20261028025431_AddInventarioCafeTerminado

  Este script crea la base desde cero con la misma estructura que generan las
  migraciones del proyecto .NET actual.

  Ejecucion (contra la instancia, normalmente master):
    sqlcmd -S localhost -E -i "C:\ProyectoCafe\tools\sqlserver-create-current.sql" -b

  Si la base ya existe, el script se detiene para evitar mezclar estructuras
  heredadas. Para recrearla de forma explicita:
    1. Cambiar @RecreateExisting a 1.
    2. Respaldar la base antes de ejecutar.

  Nota: los roles iniciales (Admin, User) los siembra la API al iniciar.
  Los usuarios de prueba (admin@local, user@local) solo se crean en Development
  a partir de hashes PBKDF2 configurados en appsettings.Development.json;
  nunca se almacenan contraseñas en texto plano. El script solo crea la estructura.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @DatabaseName sysname = N'ProyectoCafe.Dev';
DECLARE @RecreateExisting bit = 0; -- 1 = elimina y recrea la base existente.

IF DB_ID(@DatabaseName) IS NOT NULL
BEGIN
    IF @RecreateExisting = 0
    BEGIN
        THROW 51000, N'La base de datos ya existe. Cambia @RecreateExisting a 1 solo si vas a recrearla (respaldala antes).', 1;
    END;

    DECLARE @dropDatabaseSql nvarchar(max) =
        N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' + CHAR(10)
        + N'DROP DATABASE ' + QUOTENAME(@DatabaseName) + N';';

    EXEC sys.sp_executesql @dropDatabaseSql;
    PRINT N'Base anterior eliminada: ' + QUOTENAME(@DatabaseName);
END;

DECLARE @createDatabaseSql nvarchar(max) =
    N'CREATE DATABASE ' + QUOTENAME(@DatabaseName) + N';';

EXEC sys.sp_executesql @createDatabaseSql;
PRINT N'Base creada: ' + QUOTENAME(@DatabaseName);

DECLARE @schemaSql nvarchar(max) = N'
IF SCHEMA_ID(N''Seguridad'') IS NULL
BEGIN
    EXEC(N''CREATE SCHEMA Seguridad AUTHORIZATION dbo;'');
END;

IF SCHEMA_ID(N''Inventario'') IS NULL
BEGIN
    EXEC(N''CREATE SCHEMA Inventario AUTHORIZATION dbo;'');
END;

IF OBJECT_ID(N''Seguridad.Roles'', N''U'') IS NULL
BEGIN
    CREATE TABLE Seguridad.Roles
    (
        RolId int IDENTITY(1,1) NOT NULL,
        Nombre nvarchar(50) NOT NULL,
        Descripcion nvarchar(250) NULL,
        Estado int NOT NULL
            CONSTRAINT DF_Seguridad_Roles_Estado DEFAULT (0),
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RolId)
    );
END;

IF OBJECT_ID(N''Seguridad.Usuarios'', N''U'') IS NULL
BEGIN
    CREATE TABLE Seguridad.Usuarios
    (
        UsuarioId int IDENTITY(1,1) NOT NULL,
        Email nvarchar(100) NOT NULL,
        EmailNormalizado nvarchar(100) NOT NULL,
        Nombre nvarchar(150) NOT NULL,
        PasswordHash nvarchar(500) NOT NULL,
        Estado int NOT NULL
            CONSTRAINT DF_Seguridad_Usuarios_Estado DEFAULT (1),
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Seguridad_Usuarios_CreadoEn DEFAULT (sysdatetime()),
        ActualizadoEn datetime2 NULL,
        UltimoLoginEn datetime2 NULL,
        IntentosFallidosLogin int NOT NULL
            CONSTRAINT DF_Seguridad_Usuarios_IntentosFallidosLogin DEFAULT (0),
        LockoutFin datetime2 NULL,
        CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (UsuarioId)
    );
END;

IF OBJECT_ID(N''Seguridad.RolesPermisos'', N''U'') IS NULL
BEGIN
    CREATE TABLE Seguridad.RolesPermisos
    (
        RolId int NOT NULL,
        Modulo nvarchar(120) NOT NULL,
        Accion nvarchar(12) NOT NULL,
        CONSTRAINT PK_RolesPermisos PRIMARY KEY CLUSTERED (RolId, Modulo, Accion),
        CONSTRAINT FK_RolesPermisos_Roles_RolId
            FOREIGN KEY (RolId) REFERENCES Seguridad.Roles (RolId)
            ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N''Seguridad.UsuariosRoles'', N''U'') IS NULL
BEGIN
    CREATE TABLE Seguridad.UsuariosRoles
    (
        UsuarioId int NOT NULL,
        RolId int NOT NULL,
        CONSTRAINT PK_UsuariosRoles PRIMARY KEY CLUSTERED (UsuarioId, RolId),
        CONSTRAINT FK_UsuariosRoles_Roles_RolId
            FOREIGN KEY (RolId) REFERENCES Seguridad.Roles (RolId)
            ON DELETE CASCADE,
        CONSTRAINT FK_UsuariosRoles_Usuarios_UsuarioId
            FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId)
            ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N''Inventario.PresentacionesCafe'', N''U'') IS NULL
BEGIN
    CREATE TABLE Inventario.PresentacionesCafe
    (
        PresentacionCafeId int IDENTITY(1,1) NOT NULL,
        Codigo nvarchar(20) NOT NULL,
        Nombre nvarchar(80) NOT NULL,
        PesoUnitarioGramos int NOT NULL,
        Estado int NOT NULL
            CONSTRAINT DF_Inventario_PresentacionesCafe_Estado DEFAULT (1),
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_PresentacionesCafe_CreadoEn DEFAULT (sysdatetime()),
        ActualizadoEn datetime2(7) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_PresentacionesCafe PRIMARY KEY CLUSTERED (PresentacionCafeId),
        CONSTRAINT CK_Inventario_PresentacionesCafe_PesoUnitarioGramos
            CHECK (PesoUnitarioGramos > 0)
    );
END;

IF OBJECT_ID(N''Inventario.TiposCafe'', N''U'') IS NULL
BEGIN
    CREATE TABLE Inventario.TiposCafe
    (
        TipoCafeId int IDENTITY(1,1) NOT NULL,
        Codigo nvarchar(20) NOT NULL,
        Nombre nvarchar(80) NOT NULL,
        Estado int NOT NULL
            CONSTRAINT DF_Inventario_TiposCafe_Estado DEFAULT (1),
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_TiposCafe_CreadoEn DEFAULT (sysdatetime()),
        ActualizadoEn datetime2(7) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_TiposCafe PRIMARY KEY CLUSTERED (TipoCafeId)
    );
END;

IF OBJECT_ID(N''Inventario.LotesCafeTerminado'', N''U'') IS NULL
BEGIN
    CREATE TABLE Inventario.LotesCafeTerminado
    (
        LoteCafeId int IDENTITY(1,1) NOT NULL,
        CodigoLote nvarchar(50) NOT NULL,
        FechaProduccion datetime2(7) NOT NULL,
        FechaEnvasado datetime2(7) NULL,
        FechaVencimiento datetime2(7) NULL,
        Estado int NOT NULL
            CONSTRAINT DF_Inventario_LotesCafeTerminado_Estado DEFAULT (1),
        Observacion nvarchar(250) NULL,
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_LotesCafeTerminado_CreadoEn DEFAULT (sysdatetime()),
        ActualizadoEn datetime2(7) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_LotesCafeTerminado PRIMARY KEY CLUSTERED (LoteCafeId),
        CONSTRAINT CK_Inventario_LotesCafeTerminado_Fechas
            CHECK (FechaVencimiento IS NULL OR FechaVencimiento >= FechaProduccion)
    );
END;

IF OBJECT_ID(N''Inventario.LotesCafeExistencias'', N''U'') IS NULL
BEGIN
    CREATE TABLE Inventario.LotesCafeExistencias
    (
        LoteCafeExistenciaId int IDENTITY(1,1) NOT NULL,
        LoteCafeId int NOT NULL,
        PresentacionCafeId int NOT NULL,
        TipoCafeId int NOT NULL,
        CantidadPaquetes int NOT NULL
            CONSTRAINT DF_Inventario_LotesCafeExistencias_CantidadPaquetes DEFAULT (0),
        PesoUnitarioGramos int NOT NULL,
        PesoNetoKg AS
        (
            CAST((CAST(CantidadPaquetes AS decimal(18,3)) * PesoUnitarioGramos) / 1000.0 AS decimal(18,3))
        ) PERSISTED,
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_LotesCafeExistencias_CreadoEn DEFAULT (sysdatetime()),
        ActualizadoEn datetime2(7) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_LotesCafeExistencias PRIMARY KEY CLUSTERED (LoteCafeExistenciaId),
        CONSTRAINT CK_Inventario_LotesCafeExistencias_CantidadPaquetes
            CHECK (CantidadPaquetes >= 0),
        CONSTRAINT CK_Inventario_LotesCafeExistencias_PesoUnitarioGramos
            CHECK (PesoUnitarioGramos > 0),
        CONSTRAINT FK_LotesCafeExistencias_LotesCafeTerminado_LoteCafeId
            FOREIGN KEY (LoteCafeId) REFERENCES Inventario.LotesCafeTerminado (LoteCafeId)
            ON DELETE NO ACTION,
        CONSTRAINT FK_LotesCafeExistencias_PresentacionesCafe_PresentacionCafeId
            FOREIGN KEY (PresentacionCafeId) REFERENCES Inventario.PresentacionesCafe (PresentacionCafeId)
            ON DELETE NO ACTION,
        CONSTRAINT FK_LotesCafeExistencias_TiposCafe_TipoCafeId
            FOREIGN KEY (TipoCafeId) REFERENCES Inventario.TiposCafe (TipoCafeId)
            ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N''Inventario.MovimientosLoteCafe'', N''U'') IS NULL
BEGIN
    CREATE TABLE Inventario.MovimientosLoteCafe
    (
        MovimientoLoteCafeId int IDENTITY(1,1) NOT NULL,
        LoteCafeExistenciaId int NOT NULL,
        TipoMovimiento int NOT NULL,
        CantidadPaquetes int NOT NULL,
        PesoNetoKg decimal(18,3) NOT NULL,
        FechaMovimiento datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_MovimientosLoteCafe_FechaMovimiento DEFAULT (sysdatetime()),
        Referencia nvarchar(100) NULL,
        Observacion nvarchar(250) NULL,
        UsuarioId int NULL,
        CreadoEn datetime2(7) NOT NULL
            CONSTRAINT DF_Inventario_MovimientosLoteCafe_CreadoEn DEFAULT (sysdatetime()),
        CONSTRAINT PK_MovimientosLoteCafe PRIMARY KEY CLUSTERED (MovimientoLoteCafeId),
        CONSTRAINT CK_Inventario_MovimientosLoteCafe_CantidadPaquetes
            CHECK (CantidadPaquetes > 0),
        CONSTRAINT CK_Inventario_MovimientosLoteCafe_PesoNetoKg
            CHECK (PesoNetoKg > 0),
        CONSTRAINT FK_MovimientosLoteCafe_LotesCafeExistencias_LoteCafeExistenciaId
            FOREIGN KEY (LoteCafeExistenciaId) REFERENCES Inventario.LotesCafeExistencias (LoteCafeExistenciaId)
            ON DELETE NO ACTION,
        CONSTRAINT FK_MovimientosLoteCafe_Usuarios_UsuarioId
            FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId)
            ON DELETE NO ACTION
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Seguridad_Roles_Nombre''
      AND object_id = OBJECT_ID(N''Seguridad.Roles'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Seguridad_Roles_Nombre
        ON Seguridad.Roles (Nombre);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Seguridad_Usuarios_EmailNormalizado''
      AND object_id = OBJECT_ID(N''Seguridad.Usuarios'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Seguridad_Usuarios_EmailNormalizado
        ON Seguridad.Usuarios (EmailNormalizado);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_UsuariosRoles_RolId''
      AND object_id = OBJECT_ID(N''Seguridad.UsuariosRoles'')
)
BEGIN
    CREATE INDEX IX_UsuariosRoles_RolId
        ON Seguridad.UsuariosRoles (RolId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_PresentacionesCafe_Codigo''
      AND object_id = OBJECT_ID(N''Inventario.PresentacionesCafe'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_PresentacionesCafe_Codigo
        ON Inventario.PresentacionesCafe (Codigo);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_PresentacionesCafe_Nombre''
      AND object_id = OBJECT_ID(N''Inventario.PresentacionesCafe'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_PresentacionesCafe_Nombre
        ON Inventario.PresentacionesCafe (Nombre);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_TiposCafe_Codigo''
      AND object_id = OBJECT_ID(N''Inventario.TiposCafe'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_TiposCafe_Codigo
        ON Inventario.TiposCafe (Codigo);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_TiposCafe_Nombre''
      AND object_id = OBJECT_ID(N''Inventario.TiposCafe'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_TiposCafe_Nombre
        ON Inventario.TiposCafe (Nombre);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_LotesCafeTerminado_CodigoLote''
      AND object_id = OBJECT_ID(N''Inventario.LotesCafeTerminado'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_LotesCafeTerminado_CodigoLote
        ON Inventario.LotesCafeTerminado (CodigoLote);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_Inventario_LotesCafeTerminado_Estado_FechaProduccion''
      AND object_id = OBJECT_ID(N''Inventario.LotesCafeTerminado'')
)
BEGIN
    CREATE INDEX IX_Inventario_LotesCafeTerminado_Estado_FechaProduccion
        ON Inventario.LotesCafeTerminado (Estado, FechaProduccion);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo''
      AND object_id = OBJECT_ID(N''Inventario.LotesCafeExistencias'')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo
        ON Inventario.LotesCafeExistencias (LoteCafeId, PresentacionCafeId, TipoCafeId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_Inventario_LotesCafeExistencias_PresentacionTipo''
      AND object_id = OBJECT_ID(N''Inventario.LotesCafeExistencias'')
)
BEGIN
    CREATE INDEX IX_Inventario_LotesCafeExistencias_PresentacionTipo
        ON Inventario.LotesCafeExistencias (PresentacionCafeId, TipoCafeId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_LotesCafeExistencias_TipoCafeId''
      AND object_id = OBJECT_ID(N''Inventario.LotesCafeExistencias'')
)
BEGIN
    CREATE INDEX IX_LotesCafeExistencias_TipoCafeId
        ON Inventario.LotesCafeExistencias (TipoCafeId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_Inventario_MovimientosLoteCafe_ExistenciaFecha''
      AND object_id = OBJECT_ID(N''Inventario.MovimientosLoteCafe'')
)
BEGIN
    CREATE INDEX IX_Inventario_MovimientosLoteCafe_ExistenciaFecha
        ON Inventario.MovimientosLoteCafe (LoteCafeExistenciaId, FechaMovimiento);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N''IX_MovimientosLoteCafe_UsuarioId''
      AND object_id = OBJECT_ID(N''Inventario.MovimientosLoteCafe'')
)
BEGIN
    CREATE INDEX IX_MovimientosLoteCafe_UsuarioId
        ON Inventario.MovimientosLoteCafe (UsuarioId);
END;

IF NOT EXISTS (SELECT 1 FROM Inventario.PresentacionesCafe WHERE Codigo = N''250G'')
BEGIN
    INSERT INTO Inventario.PresentacionesCafe (Codigo, Nombre, PesoUnitarioGramos, Estado)
    VALUES (N''250G'', N''250 g'', 250, 1);
END;

IF NOT EXISTS (SELECT 1 FROM Inventario.PresentacionesCafe WHERE Codigo = N''454G'')
BEGIN
    INSERT INTO Inventario.PresentacionesCafe (Codigo, Nombre, PesoUnitarioGramos, Estado)
    VALUES (N''454G'', N''454 g'', 454, 1);
END;

IF NOT EXISTS (SELECT 1 FROM Inventario.TiposCafe WHERE Codigo = N''GRANO'')
BEGIN
    INSERT INTO Inventario.TiposCafe (Codigo, Nombre, Estado)
    VALUES (N''GRANO'', N''Grano'', 1);
END;

IF NOT EXISTS (SELECT 1 FROM Inventario.TiposCafe WHERE Codigo = N''MOLIDO'')
BEGIN
    INSERT INTO Inventario.TiposCafe (Codigo, Nombre, Estado)
    VALUES (N''MOLIDO'', N''Molido'', 1);
END;

IF OBJECT_ID(N''dbo.__EFMigrationsHistory'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory
    (
        MigrationId nvarchar(150) NOT NULL,
        ProductVersion nvarchar(32) NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N''20261023012804_InitialAdministration''
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N''20261023012804_InitialAdministration'', N''9.0.10'');
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N''20261028025431_AddInventarioCafeTerminado''
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N''20261028025431_AddInventarioCafeTerminado'', N''9.0.10'');
END;
';

DECLARE @schemaExecSql nvarchar(max) =
    N'EXEC ' + QUOTENAME(@DatabaseName) + N'.sys.sp_executesql @stmt;';

EXEC sys.sp_executesql
    @schemaExecSql,
    N'@stmt nvarchar(max)',
    @stmt = @schemaSql;

PRINT N'Estructura Seguridad e Inventario creada o validada en ' + QUOTENAME(@DatabaseName) + N'.';
PRINT N'Historial EF alineado a 20261028025431_AddInventarioCafeTerminado.';
