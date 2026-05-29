using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoffeeHarvests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CosechasCafe",
                schema: "Produccion",
                columns: table => new
                {
                    CosechaCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcelaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    TipoCafe = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCosecha = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Humedad = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrada"),
                    LoteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosechasCafe", x => x.CosechaCafeId);
                    table.ForeignKey(
                        name: "FK_CosechasCafe_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CosechasCafe_Parcelas_ParcelaId",
                        column: x => x.ParcelaId,
                        principalSchema: "Produccion",
                        principalTable: "Parcelas",
                        principalColumn: "ParcelaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CosechasCafe_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CosechasCafe_LoteId",
                schema: "Produccion",
                table: "CosechasCafe",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CosechasCafe_ProductoId",
                schema: "Produccion",
                table: "CosechasCafe",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_CosechasCafe_Fecha_Estado",
                schema: "Produccion",
                table: "CosechasCafe",
                columns: new[] { "FechaCosecha", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_CosechasCafe_Parcela_Fecha",
                schema: "Produccion",
                table: "CosechasCafe",
                columns: new[] { "ParcelaId", "FechaCosecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CosechasCafe",
                schema: "Produccion");
        }
    }
}
