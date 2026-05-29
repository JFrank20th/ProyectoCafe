using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoffeePurchaseReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecepcionesCafe",
                schema: "Compras",
                columns: table => new
                {
                    RecepcionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    TipoCafe = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2(7)", nullable: true, defaultValueSql: "sysdatetime()"),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Humedad = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Registrada"),
                    LoteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionesCafe", x => x.RecepcionCafeId);
                    table.ForeignKey(
                        name: "FK_RecepcionesCafe_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalSchema: "Inventario",
                        principalTable: "Lotes",
                        principalColumn: "LoteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepcionesCafe_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalSchema: "Inventario",
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepcionesCafe_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalSchema: "Compras",
                        principalTable: "Proveedores",
                        principalColumn: "ProveedorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_RecepcionesCafe_Fecha_Estado",
                schema: "Compras",
                table: "RecepcionesCafe",
                columns: new[] { "FechaRecepcion", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCafe_LoteId",
                schema: "Compras",
                table: "RecepcionesCafe",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCafe_ProductoId",
                schema: "Compras",
                table: "RecepcionesCafe",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "UX_Compras_RecepcionesCafe_Proveedor_Documento",
                schema: "Compras",
                table: "RecepcionesCafe",
                columns: new[] { "ProveedorId", "NumeroDocumento" },
                unique: true,
                filter: "[NumeroDocumento] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecepcionesCafe",
                schema: "Compras");
        }
    }
}
