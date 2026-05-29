using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPresentationPricingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TipoPrecio",
                schema: "Negocio",
                table: "Ventas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Normal",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Normal");

            migrationBuilder.AddColumn<decimal>(
                name: "CostoProceso",
                schema: "Produccion",
                table: "TransformacionesCafe",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PresentacionesCafe",
                schema: "Inventario",
                columns: table => new
                {
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PesoGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentacionesCafe", x => x.PresentacionCafeId);
                });

            migrationBuilder.CreateTable(
                name: "TiposVenta",
                schema: "Negocio",
                columns: table => new
                {
                    TipoVentaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposVenta", x => x.TipoVentaId);
                });

            migrationBuilder.CreateTable(
                name: "ProductosPreciosTipoVenta",
                schema: "Negocio",
                columns: table => new
                {
                    ProductoPrecioTipoVentaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    TipoVentaId = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosPreciosTipoVenta", x => x.ProductoPrecioTipoVentaId);
                    table.ForeignKey(
                        name: "FK_ProductosPreciosTipoVenta_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductosPreciosTipoVenta_TiposVenta_TipoVentaId",
                        column: x => x.TipoVentaId,
                        principalSchema: "Negocio",
                        principalTable: "TiposVenta",
                        principalColumn: "TipoVentaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Negocio",
                table: "TiposVenta",
                columns: new[] { "TipoVentaId", "Codigo", "Estado", "Nombre", "Orden" },
                values: new object[,]
                {
                    { 1, "Normal", true, "Precio normal", 10 },
                    { 2, "Internet", true, "Precio internet", 20 },
                    { 3, "Mayorista", true, "Precio mayorista", 30 }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [Negocio].[ProductosPreciosTipoVenta] ([ProductoId], [TipoVentaId], [Precio], [Estado])
                SELECT
                    p.[ProductoId],
                    tv.[TipoVentaId],
                    src.[Precio],
                    CAST(1 AS bit)
                FROM [Inventario].[Productos] AS p
                CROSS APPLY (VALUES
                    (N'Normal', p.[Precio]),
                    (N'Internet', p.[PrecioInternet]),
                    (N'Mayorista', p.[PrecioMayorista])
                ) AS src([Codigo], [Precio])
                INNER JOIN [Negocio].[TiposVenta] AS tv
                    ON tv.[Codigo] = src.[Codigo]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Negocio].[ProductosPreciosTipoVenta] AS existing
                    WHERE existing.[ProductoId] = p.[ProductoId]
                      AND existing.[TipoVentaId] = tv.[TipoVentaId]
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE tr
                SET tr.[CostoProceso] = ISNULL(costos.[Monto], 0)
                FROM [Produccion].[TransformacionesCafe] AS tr
                OUTER APPLY (
                    SELECT SUM(c.[Monto]) AS [Monto]
                    FROM [Contabilidad].[Costos] AS c
                    WHERE c.[FuenteModulo] = N'Produccion.TransformacionesCafe'
                      AND c.[ReferenciaId] = tr.[TransformacionCafeId]
                      AND ISNULL(c.[Estado], 1) = 1
                ) AS costos;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                column: "PresentacionCafeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_PresentacionesCafe_Codigo",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductosPreciosTipoVenta_TipoVentaId",
                schema: "Negocio",
                table: "ProductosPreciosTipoVenta",
                column: "TipoVentaId");

            migrationBuilder.CreateIndex(
                name: "UQ_Negocio_ProductosPreciosTipoVenta_Producto_TipoVenta",
                schema: "Negocio",
                table: "ProductosPreciosTipoVenta",
                columns: new[] { "ProductoId", "TipoVentaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Negocio_TiposVenta_Codigo",
                schema: "Negocio",
                table: "TiposVenta",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_PresentacionesCafe_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                column: "PresentacionCafeId",
                principalSchema: "Inventario",
                principalTable: "PresentacionesCafe",
                principalColumn: "PresentacionCafeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_PresentacionesCafe_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "PresentacionesCafe",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "ProductosPreciosTipoVenta",
                schema: "Negocio");

            migrationBuilder.DropTable(
                name: "TiposVenta",
                schema: "Negocio");

            migrationBuilder.DropIndex(
                name: "IX_Productos_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CostoProceso",
                schema: "Produccion",
                table: "TransformacionesCafe");

            migrationBuilder.DropColumn(
                name: "PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.AlterColumn<string>(
                name: "TipoPrecio",
                schema: "Negocio",
                table: "Ventas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Normal");
        }
    }
}
