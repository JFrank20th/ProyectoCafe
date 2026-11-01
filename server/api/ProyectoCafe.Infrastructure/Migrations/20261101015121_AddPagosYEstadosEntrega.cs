using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagosYEstadosEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoEntrega",
                schema: "Ventas",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "PagosVenta",
                schema: "Ventas",
                columns: table => new
                {
                    PagoVentaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosVenta", x => x.PagoVentaId);
                    table.CheckConstraint("CK_Ventas_PagosVenta_Monto", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_PagosVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalSchema: "Ventas",
                        principalTable: "Ventas",
                        principalColumn: "VentaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_PagosVenta_VentaId",
                schema: "Ventas",
                table: "PagosVenta",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosVenta",
                schema: "Ventas");

            migrationBuilder.DropColumn(
                name: "EstadoEntrega",
                schema: "Ventas",
                table: "Ventas");
        }
    }
}
