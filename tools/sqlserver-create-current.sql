/*
  Creacion de la base de datos vigente de ProyectoCafe.

  Alcance actual del proyecto:
  - Administracion (Usuarios, Roles, UsuariosRoles, RolesPermisos)
  - Historial de EF Core alineado a la linea base 20261023012804_InitialAdministration

  Este script crea la base desde cero con la misma estructura que genera la
  migracion InitialAdministration del proyecto .NET actual.

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
';

DECLARE @schemaExecSql nvarchar(max) =
    N'EXEC ' + QUOTENAME(@DatabaseName) + N'.sys.sp_executesql @stmt;';

EXEC sys.sp_executesql
    @schemaExecSql,
    N'@stmt nvarchar(max)',
    @stmt = @schemaSql;

PRINT N'Estructura Seguridad creada o validada en ' + QUOTENAME(@DatabaseName) + N'.';
PRINT N'Historial EF alineado a 20261023012804_InitialAdministration.';
