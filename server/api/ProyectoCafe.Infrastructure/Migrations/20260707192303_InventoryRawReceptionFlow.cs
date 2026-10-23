using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryRawReceptionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Compras_RecepcionesCafe_Proveedor_Documento",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.AlterColumn<int>(
                name: "LoteId",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadDisponible",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CodigoLote",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LoteOrigenId",
                schema: "Inventario",
                table: "ProcesosInventario",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [Compras].[Proveedores] ([Nombre], [Direccion], [Estado])
                SELECT DISTINCT CONCAT(f.[Nombre], N' / ', c.[Nombre]), f.[Ubicacion], 1
                FROM [Produccion].[OperacionesAgricolas] oa
                INNER JOIN [Produccion].[Cultivos] c ON c.[CultivoId] = oa.[CultivoId]
                INNER JOIN [Produccion].[Fincas] f ON f.[FincaId] = c.[FincaId]
                LEFT JOIN [Compras].[Proveedores] p ON p.[Nombre] = CONCAT(f.[Nombre], N' / ', c.[Nombre])
                WHERE oa.[TipoOperacion] = N'Cosecha'
                  AND oa.[LoteId] IS NOT NULL
                  AND p.[ProveedorId] IS NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [Compras].[RecepcionesCafe]
                    ([ProveedorId], [ProductoId], [TipoCafe], [FechaRecepcion], [CodigoLote], [Cantidad], [CantidadDisponible], [PrecioUnitario], [Total], [Humedad], [Observaciones], [Estado], [LoteId])
                SELECT p.[ProveedorId],
                       oa.[ProductoId],
                       N'Cereza',
                       COALESCE(oa.[FechaOperacion], SYSUTCDATETIME()),
                       l.[CodigoLote],
                       COALESCE(oa.[Cantidad], l.[CantidadInicial]),
                       l.[CantidadDisponible],
                       CAST(0 AS decimal(18,2)),
                       CAST(0 AS decimal(18,2)),
                       oa.[Humedad],
                       oa.[Observaciones],
                       N'Registrada',
                       oa.[LoteId]
                FROM [Produccion].[OperacionesAgricolas] oa
                INNER JOIN [Produccion].[Cultivos] c ON c.[CultivoId] = oa.[CultivoId]
                INNER JOIN [Produccion].[Fincas] f ON f.[FincaId] = c.[FincaId]
                INNER JOIN [Compras].[Proveedores] p ON p.[Nombre] = CONCAT(f.[Nombre], N' / ', c.[Nombre])
                INNER JOIN [Inventario].[Lotes] l ON l.[LoteId] = oa.[LoteId]
                LEFT JOIN [Compras].[RecepcionesCafe] r ON r.[LoteId] = oa.[LoteId]
                WHERE oa.[TipoOperacion] = N'Cosecha'
                  AND oa.[LoteId] IS NOT NULL
                  AND r.[RecepcionCafeId] IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE rc
                SET rc.[CodigoLote] = COALESCE(NULLIF(LTRIM(RTRIM(rc.[CodigoLote])), N''), l.[CodigoLote], CONCAT(N'REC-', FORMAT(rc.[RecepcionCafeId], '000000'))),
                    rc.[CantidadDisponible] = COALESCE(l.[CantidadDisponible], rc.[Cantidad])
                FROM [Compras].[RecepcionesCafe] rc
                LEFT JOIN [Inventario].[Lotes] l ON l.[LoteId] = rc.[LoteId];
                """);

            migrationBuilder.Sql(
                """
                UPDATE oa
                SET oa.[RecepcionCafeId] = rc.[RecepcionCafeId]
                FROM [Produccion].[OperacionesAgricolas] oa
                INNER JOIN [Compras].[RecepcionesCafe] rc ON rc.[LoteId] = oa.[LoteId]
                WHERE oa.[TipoOperacion] = N'Cosecha'
                  AND oa.[LoteId] IS NOT NULL
                  AND oa.[RecepcionCafeId] IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE pi
                SET pi.[RecepcionCafeId] = rc.[RecepcionCafeId]
                FROM [Inventario].[ProcesosInventario] pi
                INNER JOIN [Compras].[RecepcionesCafe] rc ON rc.[LoteId] = pi.[LoteOrigenId]
                WHERE pi.[LoteOrigenId] IS NOT NULL
                  AND pi.[RecepcionCafeId] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoLote",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCafe_ProveedorId",
                schema: "Compras",
                table: "RecepcionesCafe",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "UX_Compras_RecepcionesCafe_CodigoLote",
                schema: "Compras",
                table: "RecepcionesCafe",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosInventario_RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario",
                column: "RecepcionCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesAgricolas_RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "RecepcionCafeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperacionesAgricolas_RecepcionesCafe_RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "RecepcionCafeId",
                principalSchema: "Compras",
                principalTable: "RecepcionesCafe",
                principalColumn: "RecepcionCafeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcesosInventario_RecepcionesCafe_RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario",
                column: "RecepcionCafeId",
                principalSchema: "Compras",
                principalTable: "RecepcionesCafe",
                principalColumn: "RecepcionCafeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperacionesAgricolas_RecepcionesCafe_RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcesosInventario_RecepcionesCafe_RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario");

            migrationBuilder.DropIndex(
                name: "IX_RecepcionesCafe_ProveedorId",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.DropIndex(
                name: "UX_Compras_RecepcionesCafe_CodigoLote",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.DropIndex(
                name: "IX_ProcesosInventario_RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario");

            migrationBuilder.DropIndex(
                name: "IX_OperacionesAgricolas_RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "CantidadDisponible",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.DropColumn(
                name: "CodigoLote",
                schema: "Compras",
                table: "RecepcionesCafe");

            migrationBuilder.DropColumn(
                name: "RecepcionCafeId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.AlterColumn<int>(
                name: "RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.DropColumn(
                name: "RecepcionCafeId",
                schema: "Inventario",
                table: "ProcesosInventario");

            migrationBuilder.AlterColumn<int>(
                name: "LoteId",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                schema: "Compras",
                table: "RecepcionesCafe",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LoteOrigenId",
                schema: "Inventario",
                table: "ProcesosInventario",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Compras_RecepcionesCafe_Proveedor_Documento",
                schema: "Compras",
                table: "RecepcionesCafe",
                columns: new[] { "ProveedorId", "NumeroDocumento" },
                unique: true,
                filter: "[NumeroDocumento] IS NOT NULL");
        }
    }
}
