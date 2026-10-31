using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasPrecios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Ventas");

            migrationBuilder.CreateTable(
                name: "PreciosVenta",
                schema: "Ventas",
                columns: table => new
                {
                    PrecioVentaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciosVenta", x => x.PrecioVentaId);
                    table.ForeignKey(
                        name: "FK_PreciosVenta_PresentacionesCafe_PresentacionCafeId",
                        column: x => x.PresentacionCafeId,
                        principalSchema: "Inventario",
                        principalTable: "PresentacionesCafe",
                        principalColumn: "PresentacionCafeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Ventas_PreciosVenta_PresentacionNombre",
                schema: "Ventas",
                table: "PreciosVenta",
                columns: new[] { "PresentacionCafeId", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreciosVenta",
                schema: "Ventas");
        }
    }
}
