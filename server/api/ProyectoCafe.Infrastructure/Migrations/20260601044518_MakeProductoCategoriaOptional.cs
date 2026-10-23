using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductoCategoriaOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                schema: "Inventario",
                table: "Productos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT 1 FROM [Inventario].[Productos] WHERE [CategoriaId] IS NULL)
BEGIN
    DECLARE @FallbackCategoriaId int = (
        SELECT TOP (1) [CategoriaId]
        FROM [Inventario].[Categorias]
        ORDER BY [CategoriaId]);

    IF @FallbackCategoriaId IS NULL
    BEGIN
        INSERT INTO [Inventario].[Categorias] ([Nombre], [Descripcion], [Estado])
        VALUES (N'General', NULL, CAST(1 AS bit));

        SET @FallbackCategoriaId = CAST(SCOPE_IDENTITY() AS int);
    END

    UPDATE [Inventario].[Productos]
    SET [CategoriaId] = @FallbackCategoriaId
    WHERE [CategoriaId] IS NULL;
END");

            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                schema: "Inventario",
                table: "Productos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
