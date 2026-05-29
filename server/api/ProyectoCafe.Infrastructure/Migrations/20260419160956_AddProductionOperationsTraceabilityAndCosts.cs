using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOperationsTraceabilityAndCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                schema: "Inventario",
                table: "Insumos",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FuenteModulo",
                schema: "Contabilidad",
                table: "Costos",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoteId",
                schema: "Contabilidad",
                table: "Costos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParcelaId",
                schema: "Contabilidad",
                table: "Costos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferenciaId",
                schema: "Contabilidad",
                table: "Costos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoCosto",
                schema: "Contabilidad",
                table: "Costos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateTable(
                name: "OperacionesAgricolas",
                schema: "Produccion",
                columns: table => new
                {
                    OperacionAgricolaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcelaId = table.Column<int>(type: "int", nullable: false),
                    TipoOperacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaOperacion = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    InsumoId = table.Column<int>(type: "int", nullable: true),
                    CantidadInsumo = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    CostoInsumo = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CostoManoObra = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CostoOtros = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CostoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrada")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesAgricolas", x => x.OperacionAgricolaId);
                    table.ForeignKey(
                        name: "FK_OperacionesAgricolas_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalSchema: "Inventario",
                        principalTable: "Insumos",
                        principalColumn: "InsumoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperacionesAgricolas_Parcelas_ParcelaId",
                        column: x => x.ParcelaId,
                        principalSchema: "Produccion",
                        principalTable: "Parcelas",
                        principalColumn: "ParcelaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contabilidad_Costos_Fuente_Referencia",
                schema: "Contabilidad",
                table: "Costos",
                columns: new[] { "FuenteModulo", "ReferenciaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contabilidad_Costos_Lote_Fecha",
                schema: "Contabilidad",
                table: "Costos",
                columns: new[] { "LoteId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Contabilidad_Costos_Parcela_Fecha",
                schema: "Contabilidad",
                table: "Costos",
                columns: new[] { "ParcelaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesAgricolas_InsumoId",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_OperacionesAgricolas_Fecha_Estado",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                columns: new[] { "FechaOperacion", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Produccion_OperacionesAgricolas_Parcela_Fecha",
                schema: "Produccion",
                table: "OperacionesAgricolas",
                columns: new[] { "ParcelaId", "FechaOperacion" });

            migrationBuilder.AddForeignKey(
                name: "FK_Costos_Lotes_LoteId",
                schema: "Contabilidad",
                table: "Costos",
                column: "LoteId",
                principalSchema: "Inventario",
                principalTable: "Lotes",
                principalColumn: "LoteId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costos_Lotes_LoteId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropForeignKey(
                name: "FK_Costos_Parcelas_ParcelaId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropTable(
                name: "OperacionesAgricolas",
                schema: "Produccion");

            migrationBuilder.DropIndex(
                name: "IX_Contabilidad_Costos_Fuente_Referencia",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropIndex(
                name: "IX_Contabilidad_Costos_Lote_Fecha",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropIndex(
                name: "IX_Contabilidad_Costos_Parcela_Fecha",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                schema: "Inventario",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "FuenteModulo",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropColumn(
                name: "LoteId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropColumn(
                name: "ParcelaId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropColumn(
                name: "ReferenciaId",
                schema: "Contabilidad",
                table: "Costos");

            migrationBuilder.DropColumn(
                name: "TipoCosto",
                schema: "Contabilidad",
                table: "Costos");
        }
    }
}
