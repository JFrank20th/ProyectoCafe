using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Seguridad].[Usuarios]
                SET [Estado] = 1
                WHERE [Estado] = 0;
                """);

            migrationBuilder.DropColumn(
                name: "EmailConfirmacionTokenExpiraEn",
                schema: "Seguridad",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailConfirmacionTokenHash",
                schema: "Seguridad",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                schema: "Seguridad",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailConfirmadoEn",
                schema: "Seguridad",
                table: "Usuarios");

            migrationBuilder.AlterColumn<int>(
                name: "Estado",
                schema: "Seguridad",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Estado",
                schema: "Seguridad",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmacionTokenExpiraEn",
                schema: "Seguridad",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmacionTokenHash",
                schema: "Seguridad",
                table: "Usuarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                schema: "Seguridad",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmadoEn",
                schema: "Seguridad",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Seguridad].[Usuarios]
                SET [Estado] = 0
                WHERE [Estado] = 1;
                """);
        }
    }
}
