/*
  Limpieza operativa de ProyectoCafe para iniciar operacion de prueba.

  Preserva por defecto:
  - Seguridad.Usuarios
  - Seguridad.Roles
  - Seguridad.UsuariosRoles
  - Seguridad.RolesPermisos
  - Inventario.UnidadesMedida
  - Negocio.TiposVenta
  - Negocio.MetodosPago
  - dbo.__EFMigrationsHistory

  Vacia el resto de tablas operativas de negocio, inventario, compras,
  produccion y contabilidad.

  Mejoras incluidas:
  - Un solo plan (#ResetPlan) como fuente de verdad para contar, borrar y reseed.
  - Falla en modo seguro si aparece una tabla de dominio no contemplada.
  - Solo ejecuta DBCC CHECKIDENT cuando la tabla realmente tiene identity.
  - Mantiene modo vista previa con rollback para revisar impacto antes de confirmar.

  Personalizacion rapida:
  - Para preservar catalogos adicionales, cambia ActionName de VACIAR a PRESERVAR
    en la seccion INSERT INTO #ResetPlan.
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
DECLARE @ReseedIdentities bit = 1; -- colocar a 0 si no quieres reseed despues de limpiar

IF DB_NAME() <> @ExpectedDatabase
BEGIN
    THROW 50001, N'Abortado: la base actual no coincide con la base esperada para este script.', 1;
END;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    THROW 50002, N'Abortado: no se encontro dbo.__EFMigrationsHistory. La base no parece ser la correcta.', 1;
END;

IF OBJECT_ID(N'tempdb..#ResetPlan', N'U') IS NOT NULL
BEGIN
    DROP TABLE #ResetPlan;
END;

CREATE TABLE #ResetPlan
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ActionName nvarchar(15) NOT NULL,
    DeleteOrder int NOT NULL,
    ReseedIdentity bit NOT NULL,
    ObjectId int NULL,
    TableExists bit NULL,
    HasIdentity bit NULL,
    QualifiedName AS (QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName)),
    SortOrder AS (CASE WHEN ActionName = N'PRESERVAR' THEN 0 ELSE 1000 END + DeleteOrder),
    CONSTRAINT CK_ResetPlan_Action CHECK (ActionName IN (N'PRESERVAR', N'VACIAR'))
);

INSERT INTO #ResetPlan (SchemaName, TableName, ActionName, DeleteOrder, ReseedIdentity)
VALUES
    (N'Seguridad', N'Usuarios', N'PRESERVAR', 0, 0),
    (N'Seguridad', N'Roles', N'PRESERVAR', 0, 0),
    (N'Seguridad', N'UsuariosRoles', N'PRESERVAR', 0, 0),
    (N'Seguridad', N'RolesPermisos', N'PRESERVAR', 0, 0),
    (N'Inventario', N'UnidadesMedida', N'PRESERVAR', 0, 0),
    (N'Negocio', N'TiposVenta', N'PRESERVAR', 0, 0),
    (N'Negocio', N'MetodosPago', N'PRESERVAR', 0, 0),
    (N'dbo', N'__EFMigrationsHistory', N'PRESERVAR', 0, 0),
    (N'Negocio', N'VentaPagos', N'VACIAR', 10, 1),
    (N'Negocio', N'VentaDetalles', N'VACIAR', 20, 1),
    (N'Negocio', N'Ventas', N'VACIAR', 30, 1),
    (N'Contabilidad', N'Costos', N'VACIAR', 40, 1),
    (N'Contabilidad', N'Gastos', N'VACIAR', 50, 1),
    (N'Contabilidad', N'Pasivos', N'VACIAR', 60, 1),
    (N'Inventario', N'ProcesosInventario', N'VACIAR', 70, 1),
    (N'Produccion', N'CosechasCafe', N'VACIAR', 80, 1),
    (N'Produccion', N'OperacionesAgricolas', N'VACIAR', 90, 1),
    (N'Compras', N'RecepcionesCafe', N'VACIAR', 100, 1),
    (N'Inventario', N'LoteMovimientos', N'VACIAR', 110, 1),
    (N'Inventario', N'Lotes', N'VACIAR', 120, 1),
    (N'Negocio', N'ProductosPreciosTipoVenta', N'VACIAR', 130, 1),
    (N'Produccion', N'CultivosConfiguracionesCosecha', N'VACIAR', 133, 1),
    (N'Produccion', N'Cultivos', N'VACIAR', 135, 1),
    (N'Inventario', N'Productos', N'VACIAR', 140, 1),
    (N'Inventario', N'Insumos', N'VACIAR', 150, 1),
    (N'Negocio', N'Clientes', N'VACIAR', 180, 1),
    (N'Compras', N'Proveedores', N'VACIAR', 190, 1),
    (N'Produccion', N'Fincas', N'VACIAR', 210, 1);

IF EXISTS
(
    SELECT 1
    FROM #ResetPlan
    GROUP BY SchemaName, TableName
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 50003, N'Abortado: hay tablas duplicadas en #ResetPlan.', 1;
END;

UPDATE rp
SET
    ObjectId = tbl.object_id,
    TableExists = CASE WHEN tbl.object_id IS NULL THEN 0 ELSE 1 END,
    HasIdentity = CASE
        WHEN tbl.object_id IS NOT NULL
         AND EXISTS (SELECT 1 FROM sys.identity_columns AS ic WHERE ic.object_id = tbl.object_id)
            THEN 1
        ELSE 0
    END
FROM #ResetPlan AS rp
LEFT JOIN sys.schemas AS sch
    ON sch.name = rp.SchemaName
LEFT JOIN sys.tables AS tbl
    ON tbl.schema_id = sch.schema_id
   AND tbl.name = rp.TableName;

IF EXISTS (SELECT 1 FROM #ResetPlan WHERE TableExists = 0)
BEGIN
    PRINT N'Tablas configuradas que no existen en la base actual:';
    SELECT
        ActionName AS Accion,
        QualifiedName AS Tabla
    FROM #ResetPlan
    WHERE TableExists = 0
    ORDER BY SortOrder, QualifiedName;

    THROW 50004, N'Abortado: #ResetPlan contiene tablas que no existen en la base actual.', 1;
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
  AND sch.name IN (N'Seguridad', N'Inventario', N'Negocio', N'Compras', N'Produccion', N'Contabilidad')
  AND NOT EXISTS
  (
      SELECT 1
    FROM #ResetPlan AS rp
    WHERE rp.SchemaName = sch.name
      AND rp.TableName = tbl.name
  );

IF EXISTS (SELECT 1 FROM #UnlistedTables)
BEGIN
    PRINT N'Advertencia: hay tablas de dominio no contempladas por el plan de limpieza.';
    SELECT QualifiedName AS TablaNoContemplada
    FROM #UnlistedTables
    ORDER BY QualifiedName;

    IF @FailOnUnlistedTables = 1
    BEGIN
        THROW 50005, N'Abortado: existen tablas de dominio no contempladas por el plan de limpieza.', 1;
    END;
END;

PRINT N'Resumen del plan de limpieza:';
SELECT
    ActionName AS Accion,
    QualifiedName AS Tabla,
    CASE WHEN ActionName = N'VACIAR' THEN DeleteOrder END AS OrdenBorrado,
    CASE WHEN HasIdentity = 1 THEN N'SI' ELSE N'NO' END AS TieneIdentity,
    CASE WHEN ReseedIdentity = 1 THEN N'SI' ELSE N'NO' END AS ConsiderarReseed
FROM #ResetPlan
ORDER BY SortOrder, QualifiedName;

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
DECLARE @DeleteSql nvarchar(max);
DECLARE @ReseedSql nvarchar(max);

SELECT @CountsSql =
(
    SELECT
        N'INSERT INTO #Counts (Fase, Orden, Accion, Tabla, Registros) '
        + N'SELECT @Phase, '
        + CAST(SortOrder AS nvarchar(20))
        + N', N''' + REPLACE(ActionName, N'''', N'''''') + N''' '
        + N', N''' + REPLACE(QualifiedName, N'''', N'''''') + N''' '
        + N', COUNT_BIG(*) FROM ' + QualifiedName + N';' + CHAR(10)
    FROM #ResetPlan
    ORDER BY SortOrder, QualifiedName
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @DeleteSql =
(
    SELECT
        N'DELETE FROM ' + QualifiedName + N';' + CHAR(10)
    FROM #ResetPlan
    WHERE ActionName = N'VACIAR'
    ORDER BY DeleteOrder, QualifiedName
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

SELECT @ReseedSql =
(
    SELECT
        N'DBCC CHECKIDENT (N''' + REPLACE(QualifiedName, N'''', N'''''') + N''', RESEED, 0) WITH NO_INFOMSGS;' + CHAR(10)
    FROM #ResetPlan
    WHERE ActionName = N'VACIAR'
      AND ReseedIdentity = 1
      AND HasIdentity = 1
    ORDER BY DeleteOrder, QualifiedName
    FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(max)');

IF ISNULL(@CountsSql, N'') = N'' OR ISNULL(@DeleteSql, N'') = N''
BEGIN
    THROW 50006, N'Abortado: no fue posible construir el SQL dinamico del plan de limpieza.', 1;
END;

PRINT N'Conteos iniciales por tabla:';
EXEC sys.sp_executesql @CountsSql, N'@Phase nvarchar(40)', @Phase = @Phase;

SELECT Fase, Accion, Tabla, Registros
FROM #Counts
WHERE Fase = @Phase
ORDER BY Orden, Tabla;

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC sys.sp_executesql @DeleteSql;

    SET @Phase = N'DESPUES_EN_TRANSACCION';

    EXEC sys.sp_executesql @CountsSql, N'@Phase nvarchar(40)', @Phase = @Phase;

    PRINT N'Conteos despues del borrado, aun dentro de la transaccion:';
    SELECT Fase, Accion, Tabla, Registros
    FROM #Counts
    WHERE Fase = @Phase
    ORDER BY Orden, Tabla;

    IF @ApplyChanges = 1
    BEGIN
        IF @ReseedIdentities = 1 AND ISNULL(@ReseedSql, N'') <> N''
        BEGIN
            EXEC sys.sp_executesql @ReseedSql;
        END;

        COMMIT TRANSACTION;
        PRINT N'Limpieza confirmada.';
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