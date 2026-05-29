using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioUnidadesMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                schema: "Inventario",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Codigo);
                });

            migrationBuilder.InsertData(
                schema: "Inventario",
                table: "UnidadesMedida",
                columns: new[] { "Codigo", "Estado", "Nombre" },
                values: new object[,]
                {
                    { "KG", true, "Kilogramo" },
                    { "L", true, "Litro" },
                    { "LB", true, "Libra" },
                    { "UN", true, "Unidad" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnidadesMedida",
                schema: "Inventario");
        }
    }
}
