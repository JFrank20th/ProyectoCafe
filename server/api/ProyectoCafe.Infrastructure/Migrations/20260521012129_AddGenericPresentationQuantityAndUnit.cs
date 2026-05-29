using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenericPresentationQuantityAndUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cantidad",
                schema: "Inventario",
                table: "PresentacionesCafe",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "G");

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [Inventario].[UnidadesMedida] WHERE [Codigo] = 'G')
                BEGIN
                    INSERT INTO [Inventario].[UnidadesMedida] ([Codigo], [Estado], [Nombre])
                    VALUES ('G', 1, 'Gramo');
                END
                """);

            migrationBuilder.Sql(
                """
                UPDATE [Inventario].[PresentacionesCafe]
                SET [Cantidad] = [PesoGramos],
                    [UnidadMedida] = 'G'
                WHERE [Cantidad] = 0;
                """);

                        migrationBuilder.Sql(
                                """
                                INSERT INTO [Seguridad].[RolesPermisos] ([RolId], [Modulo], [Accion])
                                SELECT legacy.[RolId], 'Inventario.Presentaciones', legacy.[Accion]
                                FROM [Seguridad].[RolesPermisos] AS legacy
                                WHERE legacy.[Modulo] = 'Inventario.PresentacionesCafe'
                                    AND NOT EXISTS (
                                            SELECT 1
                                            FROM [Seguridad].[RolesPermisos] AS currentPerm
                                            WHERE currentPerm.[RolId] = legacy.[RolId]
                                                AND currentPerm.[Modulo] = 'Inventario.Presentaciones'
                                                AND currentPerm.[Accion] = legacy.[Accion]
                                    );
                                """);

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesCafe_UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "UnidadMedida");

            migrationBuilder.AddForeignKey(
                name: "FK_PresentacionesCafe_UnidadesMedida_UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "UnidadMedida",
                principalSchema: "Inventario",
                principalTable: "UnidadesMedida",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PresentacionesCafe_UnidadesMedida_UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe");

            migrationBuilder.DropIndex(
                name: "IX_PresentacionesCafe_UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe");

                        migrationBuilder.Sql(
                                """
                                DELETE target
                                FROM [Seguridad].[RolesPermisos] AS target
                                WHERE target.[Modulo] = 'Inventario.Presentaciones'
                                    AND EXISTS (
                                            SELECT 1
                                            FROM [Seguridad].[RolesPermisos] AS legacy
                                            WHERE legacy.[RolId] = target.[RolId]
                                                AND legacy.[Modulo] = 'Inventario.PresentacionesCafe'
                                                AND legacy.[Accion] = target.[Accion]
                                    );
                                """);

            migrationBuilder.DropColumn(
                name: "Cantidad",
                schema: "Inventario",
                table: "PresentacionesCafe");

            migrationBuilder.DropColumn(
                name: "UnidadMedida",
                schema: "Inventario",
                table: "PresentacionesCafe");
        }
    }
}
