using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTiposTuesteCafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposTuesteCafe",
                schema: "Inventario",
                columns: table => new
                {
                    TipoTuesteCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposTuesteCafe", x => x.TipoTuesteCafeId);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_TiposTuesteCafe_Codigo",
                schema: "Inventario",
                table: "TiposTuesteCafe",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_TiposTuesteCafe_Nombre",
                schema: "Inventario",
                table: "TiposTuesteCafe",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiposTuesteCafe",
                schema: "Inventario");
        }
    }
}
