using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCategoriesAndInlinePresentationIntoProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PresentacionCantidad",
                schema: "Inventario",
                table: "Productos",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentacionCodigo",
                schema: "Inventario",
                table: "Productos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentacionNombre",
                schema: "Inventario",
                table: "Productos",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentacionUnidadMedida",
                schema: "Inventario",
                table: "Productos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE producto
SET
    [PresentacionCantidad] = presentacion.[Cantidad],
    [PresentacionCodigo] = presentacion.[Codigo],
    [PresentacionNombre] = presentacion.[Nombre],
    [PresentacionUnidadMedida] = presentacion.[UnidadMedida]
FROM [Inventario].[Productos] AS producto
INNER JOIN [Inventario].[PresentacionesCafe] AS presentacion
    ON producto.[PresentacionCafeId] = presentacion.[PresentacionCafeId];");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categorias_CategoriaId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_PresentacionesCafe_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CategoriaId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PresentacionCafeId",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "Categorias",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "PresentacionesCafe",
                schema: "Inventario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PresentacionCantidad",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PresentacionCodigo",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PresentacionNombre",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PresentacionUnidadMedida",
                schema: "Inventario",
                table: "Productos");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                schema: "Inventario",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                schema: "Inventario",
                columns: table => new
                {
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.CategoriaId);
                });

            migrationBuilder.CreateTable(
                name: "PresentacionesCafe",
                schema: "Inventario",
                columns: table => new
                {
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PesoGramos = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "G")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentacionesCafe", x => x.PresentacionCafeId);
                    table.ForeignKey(
                        name: "FK_PresentacionesCafe_UnidadesMedida_UnidadMedida",
                        column: x => x.UnidadMedida,
                        principalSchema: "Inventario",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId",
                schema: "Inventario",
                table: "Productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                column: "PresentacionCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesCafe_UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "UnidadMedida");

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_PresentacionesCafe_Codigo",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categorias_CategoriaId",
                schema: "Inventario",
                table: "Productos",
                column: "CategoriaId",
                principalSchema: "Inventario",
                principalTable: "Categorias",
                principalColumn: "CategoriaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_PresentacionesCafe_PresentacionCafeId",
                schema: "Inventario",
                table: "Productos",
                column: "PresentacionCafeId",
                principalSchema: "Inventario",
                principalTable: "PresentacionesCafe",
                principalColumn: "PresentacionCafeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
