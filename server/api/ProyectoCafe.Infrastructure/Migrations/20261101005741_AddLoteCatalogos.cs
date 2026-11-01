using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoteCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LotesCafeTerminado_TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "TipoProcesoCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LotesCafeTerminado_TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "TipoTuesteCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LotesCafeTerminado_VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "VariedadCafeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LotesCafeTerminado_TiposProcesoCafe_TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "TipoProcesoCafeId",
                principalSchema: "Inventario",
                principalTable: "TiposProcesoCafe",
                principalColumn: "TipoProcesoCafeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LotesCafeTerminado_TiposTuesteCafe_TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "TipoTuesteCafeId",
                principalSchema: "Inventario",
                principalTable: "TiposTuesteCafe",
                principalColumn: "TipoTuesteCafeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LotesCafeTerminado_VariedadesCafe_VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "VariedadCafeId",
                principalSchema: "Inventario",
                principalTable: "VariedadesCafe",
                principalColumn: "VariedadCafeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LotesCafeTerminado_TiposProcesoCafe_TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropForeignKey(
                name: "FK_LotesCafeTerminado_TiposTuesteCafe_TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropForeignKey(
                name: "FK_LotesCafeTerminado_VariedadesCafe_VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropIndex(
                name: "IX_Inventario_LotesCafeTerminado_TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropIndex(
                name: "IX_Inventario_LotesCafeTerminado_TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropIndex(
                name: "IX_Inventario_LotesCafeTerminado_VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropColumn(
                name: "TipoProcesoCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropColumn(
                name: "TipoTuesteCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");

            migrationBuilder.DropColumn(
                name: "VariedadCafeId",
                schema: "Inventario",
                table: "LotesCafeTerminado");
        }
    }
}
