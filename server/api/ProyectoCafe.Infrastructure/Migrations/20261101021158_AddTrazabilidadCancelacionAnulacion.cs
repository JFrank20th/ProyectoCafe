using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrazabilidadCancelacionAnulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnuladoEn",
                schema: "Ventas",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceladoEn",
                schema: "Ventas",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                schema: "Ventas",
                table: "Ventas",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                schema: "Ventas",
                table: "Ventas",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                column: "AnuladoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                column: "CanceladoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                column: "AnuladoPorUsuarioId",
                principalSchema: "Seguridad",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas",
                column: "CanceladoPorUsuarioId",
                principalSchema: "Seguridad",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "AnuladoEn",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "AnuladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CanceladoEn",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CanceladoPorUsuarioId",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                schema: "Ventas",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                schema: "Ventas",
                table: "Ventas");
        }
    }
}
