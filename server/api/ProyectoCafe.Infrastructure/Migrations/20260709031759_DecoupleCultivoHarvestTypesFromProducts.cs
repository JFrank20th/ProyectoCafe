using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleCultivoHarvestTypesFromProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreTipoCosecha",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                ;WITH SourceData AS (
                    SELECT
                        cfg.CultivoConfiguracionCosechaId,
                        cfg.CultivoId,
                        cfg.UnidadMedidaCodigo,
                        BaseName = LEFT(COALESCE(NULLIF(LTRIM(RTRIM(p.Nombre)), ''), 'Cosecha'), 140),
                        RowNum = ROW_NUMBER() OVER (
                            PARTITION BY cfg.CultivoId, UPPER(COALESCE(NULLIF(LTRIM(RTRIM(p.Nombre)), ''), 'Cosecha')), cfg.UnidadMedidaCodigo
                            ORDER BY cfg.CultivoConfiguracionCosechaId
                        )
                    FROM Produccion.CultivosConfiguracionesCosecha AS cfg
                    LEFT JOIN Inventario.Productos AS p ON p.ProductoId = cfg.ProductoId
                )
                UPDATE cfg
                SET NombreTipoCosecha = CASE
                    WHEN src.RowNum = 1 THEN src.BaseName
                    ELSE LEFT(src.BaseName + ' #' + CAST(src.RowNum AS varchar(10)), 150)
                END
                FROM Produccion.CultivosConfiguracionesCosecha AS cfg
                INNER JOIN SourceData AS src ON src.CultivoConfiguracionCosechaId = cfg.CultivoConfiguracionCosechaId;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CultivosConfiguracionesCosecha_Productos_ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.DropIndex(
                name: "IX_CultivosConfiguracionesCosecha_ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.DropIndex(
                name: "UX_Produccion_CultivosConfiguracionesCosecha_Unica",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.CreateIndex(
                name: "UX_Produccion_CultivosConfiguracionesCosecha_Unica",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                columns: new[] { "CultivoId", "NombreTipoCosecha", "UnidadMedidaCodigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Produccion_CultivosConfiguracionesCosecha_Unica",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.DropColumn(
                name: "NombreTipoCosecha",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CultivosConfiguracionesCosecha_ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "UX_Produccion_CultivosConfiguracionesCosecha_Unica",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                columns: new[] { "CultivoId", "ProductoId", "UnidadMedidaCodigo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CultivosConfiguracionesCosecha_Productos_ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                column: "ProductoId",
                principalSchema: "Inventario",
                principalTable: "Productos",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
