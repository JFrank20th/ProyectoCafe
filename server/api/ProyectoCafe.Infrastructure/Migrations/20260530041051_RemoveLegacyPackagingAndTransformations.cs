using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPackagingAndTransformations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcesosInventario",
                schema: "Inventario",
                columns: table => new
                {
                    ProcesoInventarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteOrigenId = table.Column<int>(type: "int", nullable: false),
                    ProductoSalidaId = table.Column<int>(type: "int", nullable: false),
                    LoteSalidaId = table.Column<int>(type: "int", nullable: false),
                    UsaLoteCompleto = table.Column<bool>(type: "bit", nullable: false),
                    FechaProceso = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    CantidadOrigenConsumida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadSalida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoProceso = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesosInventario", x => x.ProcesoInventarioId);
                    table.ForeignKey(
                        name: "FK_ProcesosInventario_Lotes_LoteOrigenId",
                        column: x => x.LoteOrigenId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesosInventario_Lotes_LoteSalidaId",
                        column: x => x.LoteSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesosInventario_Productos_ProductoSalidaId",
                        column: x => x.ProductoSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_ProcesosInventario_Fecha_Estado",
                schema: "Inventario",
                table: "ProcesosInventario",
                columns: new[] { "FechaProceso", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosInventario_LoteOrigenId",
                schema: "Inventario",
                table: "ProcesosInventario",
                column: "LoteOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosInventario_LoteSalidaId",
                schema: "Inventario",
                table: "ProcesosInventario",
                column: "LoteSalidaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosInventario_ProductoSalidaId",
                schema: "Inventario",
                table: "ProcesosInventario",
                column: "ProductoSalidaId");

            migrationBuilder.Sql(
                """
                DECLARE @TransformacionesMigradas TABLE
                (
                    TransformacionCafeId int NOT NULL PRIMARY KEY,
                    ProcesoInventarioId int NOT NULL,
                    LoteSalidaId int NOT NULL
                );

                MERGE [Inventario].[ProcesosInventario] AS target
                USING
                (
                    SELECT
                        src.[TransformacionCafeId],
                        src.[LoteOrigenId],
                        src.[ProductoSalidaId],
                        src.[LoteSalidaId],
                        src.[FechaTransformacion],
                        src.[CantidadEntrada],
                        src.[CantidadSalida],
                        src.[CostoProceso],
                        src.[Observaciones],
                        src.[Estado]
                    FROM [Produccion].[TransformacionesCafe] AS src
                ) AS src
                ON 1 = 0
                WHEN NOT MATCHED THEN
                    INSERT
                    (
                        [LoteOrigenId],
                        [ProductoSalidaId],
                        [LoteSalidaId],
                        [UsaLoteCompleto],
                        [FechaProceso],
                        [CantidadOrigenConsumida],
                        [CantidadSalida],
                        [CostoProceso],
                        [Observaciones],
                        [Estado]
                    )
                    VALUES
                    (
                        src.[LoteOrigenId],
                        src.[ProductoSalidaId],
                        src.[LoteSalidaId],
                        CAST(0 AS bit),
                        src.[FechaTransformacion],
                        src.[CantidadEntrada],
                        src.[CantidadSalida],
                        src.[CostoProceso],
                        LEFT(CONCAT(N'Migrado desde TransformacionCafe #', src.[TransformacionCafeId], N'. ', COALESCE(src.[Observaciones], N'')), 250),
                        CASE
                            WHEN src.[Estado] IN (N'Anulada', N'Anulado') THEN N'Anulado'
                            ELSE N'Registrado'
                        END
                    )
                OUTPUT
                    src.[TransformacionCafeId],
                    inserted.[ProcesoInventarioId],
                    inserted.[LoteSalidaId]
                INTO @TransformacionesMigradas ([TransformacionCafeId], [ProcesoInventarioId], [LoteSalidaId]);

                UPDATE costo
                SET
                    [TipoCosto] = N'ProcesoInventario',
                    [FuenteModulo] = N'Inventario.ProcesosInventario',
                    [ReferenciaId] = migrado.[ProcesoInventarioId]
                FROM [Contabilidad].[Costos] AS costo
                INNER JOIN @TransformacionesMigradas AS migrado
                    ON migrado.[TransformacionCafeId] = costo.[ReferenciaId]
                WHERE costo.[FuenteModulo] = N'Produccion.TransformacionesCafe';

                DECLARE @EmpaquesMigrados TABLE
                (
                    EmpaqueCafeDetalleId int NOT NULL PRIMARY KEY,
                    EmpaqueCafeId int NOT NULL,
                    ProcesoInventarioId int NOT NULL,
                    LoteSalidaId int NOT NULL
                );

                MERGE [Inventario].[ProcesosInventario] AS target
                USING
                (
                    SELECT
                        detalle.[EmpaqueCafeDetalleId],
                        empaque.[EmpaqueCafeId],
                        empaque.[LoteOrigenId],
                        detalle.[ProductoSalidaId],
                        detalle.[LoteSalidaId],
                        empaque.[FechaEmpaque],
                        detalle.[CantidadOrigenAsignada],
                        detalle.[Cantidad],
                        detalle.[CostoEmpaqueAsignado],
                        empaque.[Observaciones],
                        empaque.[Estado] AS [EstadoEmpaque],
                        detalle.[Estado] AS [EstadoDetalle]
                    FROM [Produccion].[EmpaquesCafeDetalles] AS detalle
                    INNER JOIN [Produccion].[EmpaquesCafe] AS empaque
                        ON empaque.[EmpaqueCafeId] = detalle.[EmpaqueCafeId]
                ) AS src
                ON 1 = 0
                WHEN NOT MATCHED THEN
                    INSERT
                    (
                        [LoteOrigenId],
                        [ProductoSalidaId],
                        [LoteSalidaId],
                        [UsaLoteCompleto],
                        [FechaProceso],
                        [CantidadOrigenConsumida],
                        [CantidadSalida],
                        [CostoProceso],
                        [Observaciones],
                        [Estado]
                    )
                    VALUES
                    (
                        src.[LoteOrigenId],
                        src.[ProductoSalidaId],
                        src.[LoteSalidaId],
                        CAST(0 AS bit),
                        src.[FechaEmpaque],
                        src.[CantidadOrigenAsignada],
                        src.[Cantidad],
                        src.[CostoEmpaqueAsignado],
                        LEFT(CONCAT(N'Migrado desde EmpaqueCafe #', src.[EmpaqueCafeId], N' detalle #', src.[EmpaqueCafeDetalleId], N'. ', COALESCE(src.[Observaciones], N'')), 250),
                        CASE
                            WHEN src.[EstadoEmpaque] IN (N'Anulada', N'Anulado') OR src.[EstadoDetalle] IN (N'Anulada', N'Anulado') THEN N'Anulado'
                            ELSE N'Registrado'
                        END
                    )
                OUTPUT
                    src.[EmpaqueCafeDetalleId],
                    src.[EmpaqueCafeId],
                    inserted.[ProcesoInventarioId],
                    inserted.[LoteSalidaId]
                INTO @EmpaquesMigrados ([EmpaqueCafeDetalleId], [EmpaqueCafeId], [ProcesoInventarioId], [LoteSalidaId]);

                UPDATE costo
                SET
                    [TipoCosto] = N'ProcesoInventario',
                    [FuenteModulo] = N'Inventario.ProcesosInventario',
                    [ReferenciaId] = migrado.[ProcesoInventarioId]
                FROM [Contabilidad].[Costos] AS costo
                INNER JOIN @EmpaquesMigrados AS migrado
                    ON migrado.[EmpaqueCafeId] = costo.[ReferenciaId]
                    AND migrado.[LoteSalidaId] = costo.[LoteId]
                WHERE costo.[FuenteModulo] = N'Produccion.EmpaquesCafe';

                DELETE FROM [Seguridad].[RolesPermisos]
                WHERE [Modulo] IN (N'Produccion.TransformacionesCafe', N'Produccion.EmpaquesCafe');
                """);

            migrationBuilder.DropTable(
                name: "EmpaquesCafeDetalles",
                schema: "Produccion");

            migrationBuilder.DropTable(
                name: "TransformacionesCafe",
                schema: "Produccion");

            migrationBuilder.DropTable(
                name: "EmpaquesCafe",
                schema: "Produccion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcesosInventario",
                schema: "Inventario");

            migrationBuilder.CreateTable(
                name: "EmpaquesCafe",
                schema: "Produccion",
                columns: table => new
                {
                    EmpaqueCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CantidadEmpaques = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadOrigenConsumida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoEmpaque = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrado"),
                    FechaEmpaque = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    LoteOrigenId = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PesoTotalGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpaquesCafe", x => x.EmpaqueCafeId);
                    table.ForeignKey(
                        name: "FK_EmpaquesCafe_Lotes_LoteOrigenId",
                        column: x => x.LoteOrigenId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransformacionesCafe",
                schema: "Produccion",
                columns: table => new
                {
                    TransformacionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CantidadEntrada = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadSalida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoProceso = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrada"),
                    FechaTransformacion = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    LoteOrigenId = table.Column<int>(type: "int", nullable: false),
                    LoteSalidaId = table.Column<int>(type: "int", nullable: false),
                    Merma = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProductoSalidaId = table.Column<int>(type: "int", nullable: false),
                    Rendimiento = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    TipoTransformacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransformacionesCafe", x => x.TransformacionCafeId);
                    table.ForeignKey(
                        name: "FK_TransformacionesCafe_Lotes_LoteOrigenId",
                        column: x => x.LoteOrigenId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformacionesCafe_Lotes_LoteSalidaId",
                        column: x => x.LoteSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformacionesCafe_Productos_ProductoSalidaId",
                        column: x => x.ProductoSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmpaquesCafeDetalles",
                schema: "Produccion",
                columns: table => new
                {
                    EmpaqueCafeDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadOrigenAsignada = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoEmpaqueAsignado = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CostoOrigenAsignado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    EmpaqueCafeId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrado"),
                    LoteSalidaId = table.Column<int>(type: "int", nullable: false),
                    PesoGramosUnitario = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PesoTotalGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false),
                    ProductoSalidaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpaquesCafeDetalles", x => x.EmpaqueCafeDetalleId);
                    table.ForeignKey(
                        name: "FK_EmpaquesCafeDetalles_EmpaquesCafe_EmpaqueCafeId",
                        column: x => x.EmpaqueCafeId,
                        principalSchema: "Produccion",
                        principalTable: "EmpaquesCafe",
                        principalColumn: "EmpaqueCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmpaquesCafeDetalles_Lotes_LoteSalidaId",
                        column: x => x.LoteSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmpaquesCafeDetalles_PresentacionesCafe_PresentacionCafeId",
                        column: x => x.PresentacionCafeId,
                        principalSchema: "Inventario",
                        principalTable: "PresentacionesCafe",
                        principalColumn: "PresentacionCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmpaquesCafeDetalles_Productos_ProductoSalidaId",
                        column: x => x.ProductoSalidaId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmpaquesCafe_LoteOrigenId",
                schema: "Produccion",
                table: "EmpaquesCafe",
                column: "LoteOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_EmpaquesCafe_Fecha_Estado",
                schema: "Produccion",
                table: "EmpaquesCafe",
                columns: new[] { "FechaEmpaque", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_EmpaquesCafeDetalles_PresentacionCafeId",
                schema: "Produccion",
                table: "EmpaquesCafeDetalles",
                column: "PresentacionCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpaquesCafeDetalles_ProductoSalidaId",
                schema: "Produccion",
                table: "EmpaquesCafeDetalles",
                column: "ProductoSalidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_EmpaquesCafeDetalles_EmpaqueCafe",
                schema: "Produccion",
                table: "EmpaquesCafeDetalles",
                column: "EmpaqueCafeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Produccion_EmpaquesCafeDetalles_LoteSalida",
                schema: "Produccion",
                table: "EmpaquesCafeDetalles",
                column: "LoteSalidaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_TransformacionesCafe_Fecha_Estado",
                schema: "Produccion",
                table: "TransformacionesCafe",
                columns: new[] { "FechaTransformacion", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_TransformacionesCafe_LoteOrigenId",
                schema: "Produccion",
                table: "TransformacionesCafe",
                column: "LoteOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformacionesCafe_LoteSalidaId",
                schema: "Produccion",
                table: "TransformacionesCafe",
                column: "LoteSalidaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformacionesCafe_ProductoSalidaId",
                schema: "Produccion",
                table: "TransformacionesCafe",
                column: "ProductoSalidaId");
        }
    }
}
