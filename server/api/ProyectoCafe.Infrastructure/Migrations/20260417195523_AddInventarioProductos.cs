using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Productos",
                schema: "Inventario",
                columns: table => new
                {
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioInternet = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    PrecioMayorista = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Stock = table.Column<decimal>(type: "decimal(18,3)", nullable: true, defaultValue: 0m),
                    ManejaLotes = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TipoLote = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Manual"),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "UN"),
                    PermiteDecimal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.ProductoId);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalSchema: "Inventario",
                        principalTable: "Categorias",
                        principalColumn: "CategoriaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_UnidadesMedida_UnidadMedida",
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
                name: "IX_Productos_UnidadMedida",
                schema: "Inventario",
                table: "Productos",
                column: "UnidadMedida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Productos",
                schema: "Inventario");
        }
    }
}
