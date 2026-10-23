/*
  Reinicio de ProyectoCafe (alcance actual: Administración).

  Preserva:
  - Seguridad.Usuarios
  - Seguridad.Roles
  - Seguridad.UsuariosRoles
  - Seguridad.RolesPermisos

  Elimina por completo:
  - Esquemas heredados de módulos retirados: Inventario, Negocio, Compras, Produccion, Contabilidad.
  - Sus tablas, llaves foráneas y esquemas.

  Alinea el historial de migraciones con la línea base actual:
  - 20261023012804_InitialAdministration

  Modos de ejecución:
  - @ApplyChanges = 0 -> vista previa; ejecuta todo dentro de una transacción y hace ROLLBACK.
  - @ApplyChanges = 1 -> confirma los cambios.
  - @FailOnUnlistedTables = 1 -> aborta si Seguridad tiene tablas no contempladas.
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

DECLARE @ExpectedDatabase sysname = N'ProyectoCafe.Dev';
DECLARE @ApplyChanges bit = 1; -- colocar a 1 para confirmar cambios, dejar en 0 para vista previa sin cambios
DECLARE @FailOnUnlistedTables bit = 1; -- colocar a 0 si solo quieres advertencia ante tablas nuevas no listadas
DECLARE @BaselineMigrationId nvarchar(150) = N'20261023012804_InitialAdministration';
DECLARE @BaselineProductVersion nvarchar(32) = N'9.0.10';

IF DB_NAME() <> @ExpectedDatabase
BEGIN
    THROW 50001, N'Abortado: la base actual no coincide con la base esperada para este script.', 1;
END;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    THROW 50002, N'Abortado: no se encontro dbo.__EFMigrationsHistory. La base no parece ser la correcta.', 1;
END;

IF OBJECT_ID(N'tempdb..#LegacySchemas', N'U') IS NOT NULL
BEGIN
    DROP TABLE #LegacySchemas;
END;

CREATE TABLE #LegacySchemas
(
    SchemaName sysname NOT NULL PRIMARY KEY
);

INSERT INTO #LegacySchemas (SchemaName)
VALUES
    (N'Inventario'),
    (N'Negocio'),
    (N'Compras'),
    (N'Produccion'),
    (N'Contabilidad');

IF OBJECT_ID(N'tempdb..#ExpectedSeguridadTables', N'U') IS NOT NULL
BEGIN
    DROP TABLE #ExpectedSeguridadTables;
END;

CREATE TABLE #ExpectedSeguridadTables
(
    TableName sysname NOT NULL PRIMARY KEY
);

INSERT INTO #ExpectedSeguridadTables (TableName)
VALUES
    (N'Usuarios'),
    (N'Roles'),
    (N'UsuariosRoles'),
    (N'RolesPermisos');

-- La línea base asume que las tablas de Seguridad ya existen.
IF EXISTS
(
    SELECT 1
    FROM #ExpectedSeguridadTables AS expected
    WHERE OBJECT_ID(N'Seguridad.' + expected.TableName, N'U') IS NULL
)
BEGIN
    PRINT N'Tablas de Seguridad esperadas que no existen:';
    SELECT expected.TableName AS TablaFaltante
    FROM #ExpectedSeguridadTables AS expected
    WHERE OBJECT_ID(N'Seguridad.' + expected.TableName, N'U') IS NULL
    ORDER BY expected.TableName;

    THROW 50003, N'Abortado: faltan tablas de Seguridad requeridas por la línea base.', 1;
END;

IF OBJECT_ID(N'tempdb..#UnlistedTables', N'U') IS NOT NULL
BEGIN
    DROP TABLE #UnlistedTables;
END;

SELECT
    sch.name AS SchemaName,
    tbl.name AS TableName,
    QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name) AS QualifiedName
INTO #UnlistedTables
FROM sys.tables AS tbl
INNER JOIN sys.schemas AS sch
    ON sch.schema_id = tbl.schema_id
WHERE tbl.is_ms_shipped = 0
  AND sch.name = N'Seguridad'
  AND NOT EXISTS
  (
      SELECT 1
      FROM #ExpectedSeguridadTables AS expected
      WHERE expected.TableName = tbl.name
  );

IF EXISTS (SELECT 1 FROM #UnlistedTables)
BEGIN
    PRINT N'Advertencia: hay tablas de Seguridad no contempladas por la línea base.';
    SELECT QualifiedName AS TablaNoContemplada
    FROM #UnlistedTables
    ORDER BY QualifiedName;

    IF @FailOnUnlistedTables = 1
    BEGIN
        THROW 50004, N'Abortado: existen tablas de Seguridad no contempladas por la línea base.', 1;
    END;
END;

IF OBJECT_ID(N'tempdb..#LegacyObjects', N'U') IS NOT NULL
BEGIN
    DROP TABLE #LegacyObjects;
END;

SELECT
    sch.name AS SchemaName,
    obj.name AS ObjectName,
    obj.type_desc AS ObjectType,
    QUOTENAME(sch.name) + N'.' + QUOTENAME(obj.name) AS QualifiedName
INTO #LegacyObjects
FROM sys.objects AS obj
INNER JOIN sys.schemas AS sch
    ON sch.schema_id = obj.schema_id
WHERE obj.is_ms_shipped = 0
  AND sch.name IN (SELECT SchemaName FROM #LegacySchemas)
  AND obj.type IN (N'V', N'P', N'FN', N'IF', N'TF', N'TR');

IF EXISTS (SELECT 1 FROM #LegacyObjects)
BEGIN
    PRINT N'Objetos no tabulares en esquemas heredados (deben retirarse manualmente):';
    SELECT QualifiedName, ObjectType
    FROM #LegacyObjects
    ORDER BY QualifiedName;

    THROW 50005, N'Abortado: los esquemas heredados contienen vistas, procedimientos o funciones.', 1;
END;

IF OBJECT_ID(N'tempdb..#Counts', N'U') IS NOT NULL
BEGIN
    DROP TABLE #Counts;
END;

CREATE TABLE #Counts
(
    Fase nvarchar(40) NOT NULL,
    Orden int NOT NULL,
    Accion nvarchar(15) NOT NULL,
    Tabla nvarchar(300) NOT NULL,
    Registros bigint NOT NULL
);

DECLARE @Phase nvarchar(40) = N'ANTES';
DECLARE @CountsSql nvarchar(max);
DECLARE @CountsAfterSql nvarchar(max);
DECLARE @DropForeignKeysSql nvarchar(max);
DECLARE @DropTablesSql nvarchar(max);
DECLARE @DropSchemasSql nvarchar(max);

SELECT @CountsSql =
(
    SELECT
        N'INSERT INTO #Counts (Fase, Orden, Accion, Tabla, Registros) '
        + N'SELECT @Phase, '
        + CAST(CASE WHEN sch.name = N'Seguridad' THEN 0 ELSE 1000 END AS nvarchar(20))
        + N', N''' + CASE WHEN sch.name = N'Seguridad' THEN N'PRESERVAR' ELSE N'ELIMINAR' END + N''' '
        + N', N''' + REPLACE(QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name), N'''', N'''''') + N''' '
        + N', COUNT_BIG(*) FROM ' + QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name) + N';' + CHAR(10)
    FROM sys.tables AS tbl
    INNER JOIN sys.schemas AS sch
        ON sch.schema_id = tbl.schema_id
    WHERE tbl.is_ms_shipped = 0
      AND (sch.name = N'Seguridad' OR sch.name IN (SELECT SchemaName FROM #LegacySchemas))
    ORDER BY sch.name, tbl.name
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @CountsAfterSql =
(
    SELECT
        N'INSERT INTO #Counts (Fase, Orden, Accion, Tabla, Registros) '
        + N'SELECT @Phase, 0, N''PRESERVAR'' '
        + N', N''' + REPLACE(QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name), N'''', N'''''') + N''' '
        + N', COUNT_BIG(*) FROM ' + QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name) + N';' + CHAR(10)
    FROM sys.tables AS tbl
    INNER JOIN sys.schemas AS sch
        ON sch.schema_id = tbl.schema_id
    WHERE tbl.is_ms_shipped = 0
      AND sch.name = N'Seguridad'
    ORDER BY tbl.name
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @DropForeignKeysSql =
(
    SELECT
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys AS fk
    WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) IN (SELECT SchemaName FROM #LegacySchemas)
       OR OBJECT_SCHEMA_NAME(fk.referenced_object_id) IN (SELECT SchemaName FROM #LegacySchemas)
    ORDER BY fk.name
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @DropTablesSql =
(
    SELECT
        N'DROP TABLE ' + QUOTENAME(sch.name) + N'.' + QUOTENAME(tbl.name) + N';' + CHAR(10)
    FROM sys.tables AS tbl
    INNER JOIN sys.schemas AS sch
        ON sch.schema_id = tbl.schema_id
    WHERE tbl.is_ms_shipped = 0
      AND sch.name IN (SELECT SchemaName FROM #LegacySchemas)
    ORDER BY sch.name, tbl.name
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @DropSchemasSql =
(
    SELECT
        N'EXEC sys.sp_executesql N''DROP SCHEMA ' + REPLACE(QUOTENAME(sch.name), N'''', N'''''') + N';'';' + CHAR(10)
    FROM sys.schemas AS sch
    WHERE sch.name IN (SELECT SchemaName FROM #LegacySchemas)
    ORDER BY sch.name
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

IF ISNULL(@CountsSql, N'') = N'' OR ISNULL(@CountsAfterSql, N'') = N''
BEGIN
    THROW 50006, N'Abortado: no fue posible construir el SQL dinamico de conteos.', 1;
END;

PRINT N'Conteos iniciales por tabla:';
EXEC sys.sp_executesql @CountsSql, N'@Phase nvarchar(40)', @Phase = @Phase;

SELECT Fase, Accion, Tabla, Registros
FROM #Counts
WHERE Fase = @Phase
ORDER BY Orden, Tabla;

PRINT N'Plan de eliminación:';
SELECT
    sch.name AS Esquema,
    COUNT(*) AS Tablas
FROM sys.tables AS tbl
INNER JOIN sys.schemas AS sch
    ON sch.schema_id = tbl.schema_id
WHERE tbl.is_ms_shipped = 0
  AND sch.name IN (SELECT SchemaName FROM #LegacySchemas)
GROUP BY sch.name
ORDER BY sch.name;

BEGIN TRY
    BEGIN TRANSACTION;

    IF ISNULL(@DropForeignKeysSql, N'') <> N''
    BEGIN
        EXEC sys.sp_executesql @DropForeignKeysSql;
    END;

    IF ISNULL(@DropTablesSql, N'') <> N''
    BEGIN
        EXEC sys.sp_executesql @DropTablesSql;
    END;

    IF ISNULL(@DropSchemasSql, N'') <> N''
    BEGIN
        EXEC sys.sp_executesql @DropSchemasSql;
    END;

    -- Alinea el historial con la línea base actual (solo Administración).
    DELETE FROM dbo.__EFMigrationsHistory;
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (@BaselineMigrationId, @BaselineProductVersion);

    SET @Phase = N'DESPUES_EN_TRANSACCION';

    EXEC sys.sp_executesql @CountsAfterSql, N'@Phase nvarchar(40)', @Phase = @Phase;

    PRINT N'Conteos de Seguridad despues de eliminar esquemas heredados, aun dentro de la transaccion:';
    SELECT Fase, Accion, Tabla, Registros
    FROM #Counts
    WHERE Fase = @Phase
    ORDER BY Orden, Tabla;

    PRINT N'Historial de migraciones resultante:';
    SELECT MigrationId, ProductVersion
    FROM dbo.__EFMigrationsHistory
    ORDER BY MigrationId;

    IF @ApplyChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Reinicio confirmado: Seguridad preservada y esquemas heredados eliminados.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'Vista previa completada. Se ejecuto ROLLBACK porque @ApplyChanges = 0.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
