using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHarvestFieldsToAgriculturalOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cantidad",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cultivo",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Humedad",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesAgricolas_LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesAgricolas_ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperacionesAgricolas_Lotes_LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "LoteId",
                principalSchema: "Inventario",
                principalTable: "Lotes",
                principalColumn: "LoteId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperacionesAgricolas_Productos_ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "ProductoId",
                principalSchema: "Inventario",
                principalTable: "Productos",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperacionesAgricolas_Lotes_LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropForeignKey(
                name: "FK_OperacionesAgricolas_Productos_ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropIndex(
                name: "IX_OperacionesAgricolas_LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropIndex(
                name: "IX_OperacionesAgricolas_ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "Cultivo",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "Humedad",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "LoteId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                schema: "Produccion",
                table: "OperacionesAgricolas");
        }
    }
}
