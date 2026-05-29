using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioLotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lotes",
                schema: "Inventario",
                columns: table => new
                {
                    LoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CodigoLote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CantidadInicial = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Activo"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.LoteId);
                    table.ForeignKey(
                        name: "FK_Lotes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoteMovimientos",
                schema: "Inventario",
                columns: table => new
                {
                    MovimientoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoteMovimientos", x => x.MovimientoId);
                    table.ForeignKey(
                        name: "FK_LoteMovimientos_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LoteMovimientos_Lote",
                schema: "Inventario",
                table: "LoteMovimientos",
                columns: new[] { "LoteId", "MovimientoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_Lotes_Producto",
                schema: "Inventario",
                table: "Lotes",
                columns: new[] { "ProductoId", "Estado", "FechaCreacion", "LoteId" });

            migrationBuilder.CreateIndex(
                name: "UX_Inventario_Lotes_CodigoLote",
                schema: "Inventario",
                table: "Lotes",
                column: "CodigoLote",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoteMovimientos",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "Lotes",
                schema: "Inventario");
        }
    }
}
