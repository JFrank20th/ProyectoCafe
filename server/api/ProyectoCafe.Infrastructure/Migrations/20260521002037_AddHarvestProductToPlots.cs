using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHarvestProductToPlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parcelas_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas",
                column: "ProductoCosechaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcelas_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas",
                column: "ProductoCosechaId",
                principalSchema: "Inventario",
                principalTable: "Productos",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcelas_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas");

            migrationBuilder.DropIndex(
                name: "IX_Parcelas_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas");

            migrationBuilder.DropColumn(
                name: "ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas");
        }
    }
}
