using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoffeeTransformations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransformacionesCafe",
                schema: "Produccion",
                columns: table => new
                {
                    TransformacionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteOrigenId = table.Column<int>(type: "int", nullable: false),
                    ProductoSalidaId = table.Column<int>(type: "int", nullable: false),
                    LoteSalidaId = table.Column<int>(type: "int", nullable: false),
                    TipoTransformacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaTransformacion = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    CantidadEntrada = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadSalida = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Merma = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Rendimiento = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrada")
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransformacionesCafe",
                schema: "Produccion");
        }
    }
}
