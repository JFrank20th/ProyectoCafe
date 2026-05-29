using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingAndSalesCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                schema: "Negocio",
                table: "VentaDetalles",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "EmpaquesCafe",
                schema: "Produccion",
                columns: table => new
                {
                    EmpaqueCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteOrigenId = table.Column<int>(type: "int", nullable: false),
                    FechaEmpaque = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    CantidadOrigenConsumida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadEmpaques = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PesoTotalGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoEmpaque = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrado")
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
                name: "EmpaquesCafeDetalles",
                schema: "Produccion",
                columns: table => new
                {
                    EmpaqueCafeDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpaqueCafeId = table.Column<int>(type: "int", nullable: false),
                    ProductoSalidaId = table.Column<int>(type: "int", nullable: false),
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false),
                    LoteSalidaId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PesoGramosUnitario = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PesoTotalGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadOrigenAsignada = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoOrigenAsignado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CostoEmpaqueAsignado = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrado")
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmpaquesCafeDetalles",
                schema: "Produccion");

            migrationBuilder.DropTable(
                name: "EmpaquesCafe",
                schema: "Produccion");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                schema: "Negocio",
                table: "VentaDetalles");
        }
    }
}
