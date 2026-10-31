using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ventas",
                schema: "Ventas",
                columns: table => new
                {
                    VentaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroVenta = table.Column<int>(type: "int", nullable: false),
                    FechaVenta = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.VentaId);
                    table.ForeignKey(
                        name: "FK_Ventas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalSchema: "Ventas",
                        principalTable: "Clientes",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaDetalles",
                schema: "Ventas",
                columns: table => new
                {
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false),
                    TipoCafeId = table.Column<int>(type: "int", nullable: false),
                    PrecioVentaId = table.Column<int>(type: "int", nullable: false),
                    CantidadPaquetes = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDetalles", x => x.VentaDetalleId);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_PreciosVenta_PrecioVentaId",
                        column: x => x.PrecioVentaId,
                        principalSchema: "Ventas",
                        principalTable: "PreciosVenta",
                        principalColumn: "PrecioVentaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_PresentacionesCafe_PresentacionCafeId",
                        column: x => x.PresentacionCafeId,
                        principalSchema: "Inventario",
                        principalTable: "PresentacionesCafe",
                        principalColumn: "PresentacionCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_TiposCafe_TipoCafeId",
                        column: x => x.TipoCafeId,
                        principalSchema: "Inventario",
                        principalTable: "TiposCafe",
                        principalColumn: "TipoCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalSchema: "Ventas",
                        principalTable: "Ventas",
                        principalColumn: "VentaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_PrecioVentaId",
                schema: "Ventas",
                table: "VentaDetalles",
                column: "PrecioVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_PresentacionCafeId",
                schema: "Ventas",
                table: "VentaDetalles",
                column: "PresentacionCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_TipoCafeId",
                schema: "Ventas",
                table: "VentaDetalles",
                column: "TipoCafeId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_VentaId",
                schema: "Ventas",
                table: "VentaDetalles",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                schema: "Ventas",
                table: "Ventas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Ventas_FechaVenta",
                schema: "Ventas",
                table: "Ventas",
                column: "FechaVenta");

            migrationBuilder.CreateIndex(
                name: "UQ_Ventas_Ventas_NumeroVenta",
                schema: "Ventas",
                table: "Ventas",
                column: "NumeroVenta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VentaDetalles",
                schema: "Ventas");

            migrationBuilder.DropTable(
                name: "Ventas",
                schema: "Ventas");
        }
    }
}
