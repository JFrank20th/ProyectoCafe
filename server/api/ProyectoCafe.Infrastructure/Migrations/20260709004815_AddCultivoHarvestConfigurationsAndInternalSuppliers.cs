using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivoHarvestConfigurationsAndInternalSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsInterno",
                schema: "Compras",
                table: "Proveedores",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CultivosConfiguracionesCosecha",
                schema: "Produccion",
                columns: table => new
                {
                    CultivoConfiguracionCosechaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CultivoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    UnidadMedidaCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivosConfiguracionesCosecha", x => x.CultivoConfiguracionCosechaId);
                    table.ForeignKey(
                        name: "FK_CultivosConfiguracionesCosecha_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalSchema: "Produccion",
                        principalTable: "Cultivos",
                        principalColumn: "CultivoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CultivosConfiguracionesCosecha_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivosConfiguracionesCosecha_UnidadesMedida_UnidadMedidaCodigo",
                        column: x => x.UnidadMedidaCodigo,
                        principalSchema: "Inventario",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CultivosConfiguracionesCosecha_ProductoId",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivosConfiguracionesCosecha_UnidadMedidaCodigo",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                column: "UnidadMedidaCodigo");

            migrationBuilder.CreateIndex(
                name: "UX_Produccion_CultivosConfiguracionesCosecha_Unica",
                schema: "Produccion",
                table: "CultivosConfiguracionesCosecha",
                columns: new[] { "CultivoId", "ProductoId", "UnidadMedidaCodigo" },
                unique: true);

                        migrationBuilder.Sql(
                                """
                                INSERT INTO Produccion.CultivosConfiguracionesCosecha (CultivoId, ProductoId, UnidadMedidaCodigo)
                                SELECT c.CultivoId, c.ProductoCosechaId, UPPER(LTRIM(RTRIM(p.UnidadMedida)))
                                FROM Produccion.Cultivos AS c
                                INNER JOIN Inventario.Productos AS p ON p.ProductoId = c.ProductoCosechaId
                                WHERE c.ProductoCosechaId IS NOT NULL
                                    AND LTRIM(RTRIM(ISNULL(p.UnidadMedida, ''))) <> ''
                                    AND NOT EXISTS (
                                            SELECT 1
                                            FROM Produccion.CultivosConfiguracionesCosecha AS cfg
                                            WHERE cfg.CultivoId = c.CultivoId
                                                AND cfg.ProductoId = c.ProductoCosechaId
                                                AND cfg.UnidadMedidaCodigo = UPPER(LTRIM(RTRIM(p.UnidadMedida)))
                                    );
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CultivosConfiguracionesCosecha",
                schema: "Produccion");

            migrationBuilder.DropColumn(
                name: "EsInterno",
                schema: "Compras",
                table: "Proveedores");
        }
    }
}
