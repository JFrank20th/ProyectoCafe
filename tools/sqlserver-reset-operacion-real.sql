/*
  Limpieza operativa de ProyectoCafe para inicio de operacion real.

  Preserva:
  - Seguridad.Usuarios
  - Seguridad.Roles
  - Seguridad.UsuariosRoles
  - Seguridad.RolesPermisos
  - Inventario.UnidadesMedida
  - Negocio.TiposVenta
  - Negocio.MetodosPago
  - dbo.__EFMigrationsHistory

  Elimina el resto de tablas de negocio, inventario, compras, produccion y contabilidad.

  Uso recomendado:
  1. Ejecuta primero con @ApplyChanges = 0 para ver el impacto sin confirmar cambios.
  2. Revisa los conteos de tablas preservadas y vaciadas.
  3. Cambia @ApplyChanges = 1 y ejecuta de nuevo para confirmar.
  4. Toma backup antes de usar este script.

  Nota importante:
  - La API puede volver a asegurar usuarios bootstrap definidos en configuracion al arrancar.
  - Este script deja el sistema tecnicamente funcional, pero sin catalogos maestros de negocio.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'ProyectoCafe.Dev';
DECLARE @ApplyChanges bit = 0; -- colocar a 1 para confirmar cambios, dejar en 0 para vista previa sin cambios

IF DB_NAME() <> @ExpectedDatabase
BEGIN
    THROW 50001, N'Abortado: la base actual no coincide con la base esperada para este script.', 1;
END;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    THROW 50002, N'Abortado: no se encontro dbo.__EFMigrationsHistory. La base no parece ser la correcta.', 1;
END;

DECLARE @Tables TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ActionName nvarchar(15) NOT NULL,
    DeleteOrder int NOT NULL,
    ReseedIdentity bit NOT NULL
);

INSERT INTO @Tables (SchemaName, TableName, ActionName, DeleteOrder, ReseedIdentity)
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
    (N'Produccion', N'EmpaquesCafeDetalles', N'VACIAR', 70, 1),
    (N'Produccion', N'EmpaquesCafe', N'VACIAR', 80, 1),
    (N'Produccion', N'TransformacionesCafe', N'VACIAR', 90, 1),
    (N'Produccion', N'CosechasCafe', N'VACIAR', 100, 1),
    (N'Produccion', N'OperacionesAgricolas', N'VACIAR', 110, 1),
    (N'Compras', N'RecepcionesCafe', N'VACIAR', 120, 1),
    (N'Inventario', N'LoteMovimientos', N'VACIAR', 130, 1),
    (N'Inventario', N'Lotes', N'VACIAR', 140, 1),
    (N'Negocio', N'ProductosPreciosTipoVenta', N'VACIAR', 150, 1),
    (N'Inventario', N'Productos', N'VACIAR', 160, 1),
    (N'Inventario', N'Insumos', N'VACIAR', 170, 1),
    (N'Inventario', N'PresentacionesCafe', N'VACIAR', 180, 1),
    (N'Inventario', N'Categorias', N'VACIAR', 190, 1),
    (N'Negocio', N'Clientes', N'VACIAR', 200, 1),
    (N'Compras', N'Proveedores', N'VACIAR', 210, 1),
    (N'Produccion', N'Parcelas', N'VACIAR', 220, 1),
    (N'Produccion', N'Fincas', N'VACIAR', 230, 1);

DECLARE @Phase nvarchar(40);
DECLARE @CountsUnion nvarchar(max);
DECLARE @CountsSql nvarchar(max);
DECLARE @SchemaName sysname;
DECLARE @TableName sysname;
DECLARE @ActionName nvarchar(15);
DECLARE @SortOrder int;
DECLARE @QualifiedName nvarchar(300);

SET @Phase = N'ANTES';
SET @CountsUnion = N'';

DECLARE counts_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT
        SchemaName,
        TableName,
        ActionName,
        CASE WHEN ActionName = N'PRESERVAR' THEN 0 ELSE 1000 END + DeleteOrder AS SortOrder
    FROM @Tables
    ORDER BY
        CASE WHEN ActionName = N'PRESERVAR' THEN 0 ELSE 1 END,
        DeleteOrder,
        SchemaName,
        TableName;

OPEN counts_cursor;

FETCH NEXT FROM counts_cursor INTO @SchemaName, @TableName, @ActionName, @SortOrder;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @QualifiedName = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

    SET @CountsUnion = @CountsUnion
        + CASE WHEN LEN(@CountsUnion) = 0 THEN N'' ELSE N' UNION ALL ' END
        + N'SELECT '
        + N'N''' + REPLACE(@Phase, N'''', N'''''') + N''' AS Fase, '
        + CAST(@SortOrder AS nvarchar(20)) + N' AS Orden, '
        + N'N''' + REPLACE(@ActionName, N'''', N'''''') + N''' AS Accion, '
        + N'N''' + REPLACE(@QualifiedName, N'''', N'''''') + N''' AS Tabla, '
        + N'COUNT_BIG(*) AS Registros '
        + N'FROM ' + @QualifiedName;

    FETCH NEXT FROM counts_cursor INTO @SchemaName, @TableName, @ActionName, @SortOrder;
END;

CLOSE counts_cursor;
DEALLOCATE counts_cursor;

PRINT N'Conteos iniciales por tabla:';
SET @CountsSql = N'SELECT Fase, Accion, Tabla, Registros FROM (' + @CountsUnion + N') AS Conteos ORDER BY Orden, Tabla;';
EXEC sys.sp_executesql @CountsSql;

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM [Negocio].[VentaPagos];
    DELETE FROM [Negocio].[VentaDetalles];
    DELETE FROM [Negocio].[Ventas];
    DELETE FROM [Contabilidad].[Costos];
    DELETE FROM [Contabilidad].[Gastos];
    DELETE FROM [Contabilidad].[Pasivos];
    DELETE FROM [Produccion].[EmpaquesCafeDetalles];
    DELETE FROM [Produccion].[EmpaquesCafe];
    DELETE FROM [Produccion].[TransformacionesCafe];
    DELETE FROM [Produccion].[CosechasCafe];
    DELETE FROM [Produccion].[OperacionesAgricolas];
    DELETE FROM [Compras].[RecepcionesCafe];
    DELETE FROM [Inventario].[LoteMovimientos];
    DELETE FROM [Inventario].[Lotes];
    DELETE FROM [Negocio].[ProductosPreciosTipoVenta];
    DELETE FROM [Inventario].[Productos];
    DELETE FROM [Inventario].[Insumos];
    DELETE FROM [Inventario].[PresentacionesCafe];
    DELETE FROM [Inventario].[Categorias];
    DELETE FROM [Negocio].[Clientes];
    DELETE FROM [Compras].[Proveedores];
    DELETE FROM [Produccion].[Parcelas];
    DELETE FROM [Produccion].[Fincas];

    SET @Phase = N'DESPUES_EN_TRANSACCION';
    SET @CountsUnion = N'';

    DECLARE counts_cursor_after CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            SchemaName,
            TableName,
            ActionName,
            CASE WHEN ActionName = N'PRESERVAR' THEN 0 ELSE 1000 END + DeleteOrder AS SortOrder
        FROM @Tables
        ORDER BY
            CASE WHEN ActionName = N'PRESERVAR' THEN 0 ELSE 1 END,
            DeleteOrder,
            SchemaName,
            TableName;

    OPEN counts_cursor_after;

    FETCH NEXT FROM counts_cursor_after INTO @SchemaName, @TableName, @ActionName, @SortOrder;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @QualifiedName = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

        SET @CountsUnion = @CountsUnion
            + CASE WHEN LEN(@CountsUnion) = 0 THEN N'' ELSE N' UNION ALL ' END
            + N'SELECT '
            + N'N''' + REPLACE(@Phase, N'''', N'''''') + N''' AS Fase, '
            + CAST(@SortOrder AS nvarchar(20)) + N' AS Orden, '
            + N'N''' + REPLACE(@ActionName, N'''', N'''''') + N''' AS Accion, '
            + N'N''' + REPLACE(@QualifiedName, N'''', N'''''') + N''' AS Tabla, '
            + N'COUNT_BIG(*) AS Registros '
            + N'FROM ' + @QualifiedName;

        FETCH NEXT FROM counts_cursor_after INTO @SchemaName, @TableName, @ActionName, @SortOrder;
    END;

    CLOSE counts_cursor_after;
    DEALLOCATE counts_cursor_after;

    PRINT N'Conteos despues del borrado, aun dentro de la transaccion:';
    SET @CountsSql = N'SELECT Fase, Accion, Tabla, Registros FROM (' + @CountsUnion + N') AS Conteos ORDER BY Orden, Tabla;';
    EXEC sys.sp_executesql @CountsSql;

    IF @ApplyChanges = 1
    BEGIN
        DBCC CHECKIDENT (N'[Negocio].[VentaPagos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Negocio].[VentaDetalles]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Negocio].[Ventas]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Contabilidad].[Costos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Contabilidad].[Gastos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Contabilidad].[Pasivos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[EmpaquesCafeDetalles]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[EmpaquesCafe]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[TransformacionesCafe]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[CosechasCafe]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[OperacionesAgricolas]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Compras].[RecepcionesCafe]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[LoteMovimientos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[Lotes]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Negocio].[ProductosPreciosTipoVenta]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[Productos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[Insumos]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[PresentacionesCafe]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Inventario].[Categorias]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Negocio].[Clientes]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Compras].[Proveedores]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[Parcelas]', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT (N'[Produccion].[Fincas]', RESEED, 0) WITH NO_INFOMSGS;

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