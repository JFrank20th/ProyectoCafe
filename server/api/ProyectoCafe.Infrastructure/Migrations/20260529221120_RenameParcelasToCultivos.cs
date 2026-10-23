using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCultivosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CosechasCafe_Parcelas_ParcelaId",
                schema: "Produccion",
                table: "CosechasCafe");

            migrationBuilder.DropForeignKey(
                name: "FK_Costos_Parcelas_ParcelaId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropForeignKey(
                name: "FK_OperacionesAgricolas_Parcelas_ParcelaId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcelas_Fincas_FincaId",
                schema: "Produccion",
                table: "Parcelas");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcelas_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parcelas",
                schema: "Produccion",
                table: "Parcelas");

            migrationBuilder.RenameTable(
                name: "Parcelas",
                schema: "Produccion",
                newName: "Cultivos",
                newSchema: "Produccion");

            migrationBuilder.RenameColumn(
                name: "ParcelaId",
                schema: "Produccion",
                table: "Cultivos",
                newName: "CultivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Parcelas_ProductoCosechaId",
                schema: "Produccion",
                table: "Cultivos",
                newName: "IX_Cultivos_ProductoCosechaId");

            migrationBuilder.RenameIndex(
                name: "UX_Produccion_Parcelas_Finca_Nombre",
                schema: "Produccion",
                table: "Cultivos",
                newName: "UX_Produccion_Cultivos_Finca_Nombre");

            migrationBuilder.RenameColumn(
                name: "ParcelaId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                newName: "CultivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Produccion_OperacionesAgricolas_Parcela_Fecha",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                newName: "IX_Produccion_OperacionesAgricolas_Cultivo_Fecha");

            migrationBuilder.RenameColumn(
                name: "ParcelaId",
                schema: "Contabilidad",
                table: "Costos",
                newName: "CultivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Contabilidad_Costos_Parcela_Fecha",
                schema: "Contabilidad",
                table: "Costos",
                newName: "IX_Contabilidad_Costos_Cultivo_Fecha");

            migrationBuilder.RenameColumn(
                name: "ParcelaId",
                schema: "Produccion",
                table: "CosechasCafe",
                newName: "CultivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Produccion_CosechasCafe_Parcela_Fecha",
                schema: "Produccion",
                table: "CosechasCafe",
                newName: "IX_Produccion_CosechasCafe_Cultivo_Fecha");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cultivos",
                schema: "Produccion",
                table: "Cultivos",
                column: "CultivoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cultivos_Fincas_FincaId",
                schema: "Produccion",
                table: "Cultivos",
                column: "FincaId",
                principalSchema: "Produccion",
                principalTable: "Fincas",
                principalColumn: "FincaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cultivos_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Cultivos",
                column: "ProductoCosechaId",
                principalSchema: "Inventario",
                principalTable: "Productos",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CosechasCafe_Cultivos_CultivoId",
                schema: "Produccion",
                table: "CosechasCafe",
                column: "CultivoId",
                principalSchema: "Produccion",
                principalTable: "Cultivos",
                principalColumn: "CultivoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Costos_Cultivos_CultivoId",
                schema: "Contabilidad",
                table: "Costos",
                column: "CultivoId",
                principalSchema: "Produccion",
                principalTable: "Cultivos",
                principalColumn: "CultivoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperacionesAgricolas_Cultivos_CultivoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "CultivoId",
                principalSchema: "Produccion",
                principalTable: "Cultivos",
                principalColumn: "CultivoId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CosechasCafe_Cultivos_CultivoId",
                schema: "Produccion",
                table: "CosechasCafe");

            migrationBuilder.DropForeignKey(
                name: "FK_Costos_Cultivos_CultivoId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropForeignKey(
                name: "FK_OperacionesAgricolas_Cultivos_CultivoId",
                schema: "Produccion",
                table: "OperacionesAgricolas");

            migrationBuilder.DropForeignKey(
                name: "FK_Cultivos_Fincas_FincaId",
                schema: "Produccion",
                table: "Cultivos");

            migrationBuilder.DropForeignKey(
                name: "FK_Cultivos_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Cultivos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cultivos",
                schema: "Produccion",
                table: "Cultivos");

            migrationBuilder.RenameColumn(
                name: "CultivoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                newName: "ParcelaId");

            migrationBuilder.RenameIndex(
                name: "IX_Produccion_OperacionesAgricolas_Cultivo_Fecha",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                newName: "IX_Produccion_OperacionesAgricolas_Parcela_Fecha");

            migrationBuilder.RenameColumn(
                name: "CultivoId",
                schema: "Contabilidad",
                table: "Costos",
                newName: "ParcelaId");

            migrationBuilder.RenameIndex(
                name: "IX_Contabilidad_Costos_Cultivo_Fecha",
                schema: "Contabilidad",
                table: "Costos",
                newName: "IX_Contabilidad_Costos_Parcela_Fecha");

            migrationBuilder.RenameColumn(
                name: "CultivoId",
                schema: "Produccion",
                table: "CosechasCafe",
                newName: "ParcelaId");

            migrationBuilder.RenameIndex(
                name: "IX_Produccion_CosechasCafe_Cultivo_Fecha",
                schema: "Produccion",
                table: "CosechasCafe",
                newName: "IX_Produccion_CosechasCafe_Parcela_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_Cultivos_ProductoCosechaId",
                schema: "Produccion",
                table: "Cultivos",
                newName: "IX_Parcelas_ProductoCosechaId");

            migrationBuilder.RenameIndex(
                name: "UX_Produccion_Cultivos_Finca_Nombre",
                schema: "Produccion",
                table: "Cultivos",
                newName: "UX_Produccion_Parcelas_Finca_Nombre");

            migrationBuilder.RenameColumn(
                name: "CultivoId",
                schema: "Produccion",
                table: "Cultivos",
                newName: "ParcelaId");

            migrationBuilder.RenameTable(
                name: "Cultivos",
                schema: "Produccion",
                newName: "Parcelas",
                newSchema: "Produccion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parcelas",
                schema: "Produccion",
                table: "Parcelas",
                column: "ParcelaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcelas_Fincas_FincaId",
                schema: "Produccion",
                table: "Parcelas",
                column: "FincaId",
                principalSchema: "Produccion",
                principalTable: "Fincas",
                principalColumn: "FincaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcelas_Productos_ProductoCosechaId",
                schema: "Produccion",
                table: "Parcelas",
                column: "ProductoCosechaId",
                principalSchema: "Inventario",
                principalTable: "Productos",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CosechasCafe_Parcelas_ParcelaId",
                schema: "Produccion",
                table: "CosechasCafe",
                column: "ParcelaId",
                principalSchema: "Produccion",
                principalTable: "Parcelas",
                principalColumn: "ParcelaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Costos_Parcelas_ParcelaId",
                schema: "Contabilidad",
                table: "Costos",
                column: "ParcelaId",
                principalSchema: "Produccion",
                principalTable: "Parcelas",
                principalColumn: "ParcelaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperacionesAgricolas_Parcelas_ParcelaId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "ParcelaId",
                principalSchema: "Produccion",
                principalTable: "Parcelas",
                principalColumn: "ParcelaId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
