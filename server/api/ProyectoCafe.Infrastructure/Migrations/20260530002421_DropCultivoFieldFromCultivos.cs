using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCultivoFieldFromCultivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariedadCafe",
                schema: "Produccion",
                table: "Cultivos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariedadCafe",
                schema: "Produccion",
                table: "Cultivos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
