using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCafe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioCafeTerminado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Inventario");

            migrationBuilder.CreateTable(
                name: "LotesCafeTerminado",
                schema: "Inventario",
                columns: table => new
                {
                    LoteCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoLote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaProduccion = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    FechaEnvasado = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesCafeTerminado", x => x.LoteCafeId);
                    table.CheckConstraint("CK_Inventario_LotesCafeTerminado_Fechas", "[FechaVencimiento] IS NULL OR [FechaVencimiento] >= [FechaProduccion]");
                });

            migrationBuilder.CreateTable(
                name: "PresentacionesCafe",
                schema: "Inventario",
                columns: table => new
                {
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PesoUnitarioGramos = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentacionesCafe", x => x.PresentacionCafeId);
                    table.CheckConstraint("CK_Inventario_PresentacionesCafe_PesoUnitarioGramos", "[PesoUnitarioGramos] > 0");
                });

            migrationBuilder.CreateTable(
                name: "TiposCafe",
                schema: "Inventario",
                columns: table => new
                {
                    TipoCafeId = table.Column<int>(type: "int", nullable: false)
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
                    table.PrimaryKey("PK_TiposCafe", x => x.TipoCafeId);
                });

            migrationBuilder.CreateTable(
                name: "LotesCafeExistencias",
                schema: "Inventario",
                columns: table => new
                {
                    LoteCafeExistenciaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteCafeId = table.Column<int>(type: "int", nullable: false),
                    PresentacionCafeId = table.Column<int>(type: "int", nullable: false),
                    TipoCafeId = table.Column<int>(type: "int", nullable: false),
                    CantidadPaquetes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PesoUnitarioGramos = table.Column<int>(type: "int", nullable: false),
                    PesoNetoKg = table.Column<decimal>(type: "decimal(18,3)", nullable: false, computedColumnSql: "CAST((CAST([CantidadPaquetes] AS decimal(18,3)) * [PesoUnitarioGramos]) / 1000.0 AS decimal(18,3))", stored: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesCafeExistencias", x => x.LoteCafeExistenciaId);
                    table.CheckConstraint("CK_Inventario_LotesCafeExistencias_CantidadPaquetes", "[CantidadPaquetes] >= 0");
                    table.CheckConstraint("CK_Inventario_LotesCafeExistencias_PesoUnitarioGramos", "[PesoUnitarioGramos] > 0");
                    table.ForeignKey(
                        name: "FK_LotesCafeExistencias_LotesCafeTerminado_LoteCafeId",
                        column: x => x.LoteCafeId,
                        principalSchema: "Inventario",
                        principalTable: "LotesCafeTerminado",
                        principalColumn: "LoteCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesCafeExistencias_PresentacionesCafe_PresentacionCafeId",
                        column: x => x.PresentacionCafeId,
                        principalSchema: "Inventario",
                        principalTable: "PresentacionesCafe",
                        principalColumn: "PresentacionCafeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesCafeExistencias_TiposCafe_TipoCafeId",
                        column: x => x.TipoCafeId,
                        principalSchema: "Inventario",
                        principalTable: "TiposCafe",
                        principalColumn: "TipoCafeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosLoteCafe",
                schema: "Inventario",
                columns: table => new
                {
                    MovimientoLoteCafeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteCafeExistenciaId = table.Column<int>(type: "int", nullable: false),
                    TipoMovimiento = table.Column<int>(type: "int", nullable: false),
                    CantidadPaquetes = table.Column<int>(type: "int", nullable: false),
                    PesoNetoKg = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    FechaMovimiento = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()"),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "sysdatetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosLoteCafe", x => x.MovimientoLoteCafeId);
                    table.CheckConstraint("CK_Inventario_MovimientosLoteCafe_CantidadPaquetes", "[CantidadPaquetes] > 0");
                    table.CheckConstraint("CK_Inventario_MovimientosLoteCafe_PesoNetoKg", "[PesoNetoKg] > 0");
                    table.ForeignKey(
                        name: "FK_MovimientosLoteCafe_LotesCafeExistencias_LoteCafeExistenciaId",
                        column: x => x.LoteCafeExistenciaId,
                        principalSchema: "Inventario",
                        principalTable: "LotesCafeExistencias",
                        principalColumn: "LoteCafeExistenciaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosLoteCafe_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "Seguridad",
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LotesCafeExistencias_PresentacionTipo",
                schema: "Inventario",
                table: "LotesCafeExistencias",
                columns: new[] { "PresentacionCafeId", "TipoCafeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesCafeExistencias_TipoCafeId",
                schema: "Inventario",
                table: "LotesCafeExistencias",
                column: "TipoCafeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_LotesCafeExistencias_LotePresentacionTipo",
                schema: "Inventario",
                table: "LotesCafeExistencias",
                columns: new[] { "LoteCafeId", "PresentacionCafeId", "TipoCafeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LotesCafeTerminado_Estado_FechaProduccion",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                columns: new[] { "Estado", "FechaProduccion" });

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_LotesCafeTerminado_CodigoLote",
                schema: "Inventario",
                table: "LotesCafeTerminado",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_MovimientosLoteCafe_ExistenciaFecha",
                schema: "Inventario",
                table: "MovimientosLoteCafe",
                columns: new[] { "LoteCafeExistenciaId", "FechaMovimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLoteCafe_UsuarioId",
                schema: "Inventario",
                table: "MovimientosLoteCafe",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_PresentacionesCafe_Codigo",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_PresentacionesCafe_Nombre",
                schema: "Inventario",
                table: "PresentacionesCafe",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_TiposCafe_Codigo",
                schema: "Inventario",
                table: "TiposCafe",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Inventario_TiposCafe_Nombre",
                schema: "Inventario",
                table: "TiposCafe",
                column: "Nombre",
                unique: true);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [Inventario].[PresentacionesCafe] WHERE [Codigo] = N'250G')
                BEGIN
                    INSERT INTO [Inventario].[PresentacionesCafe] ([Codigo], [Nombre], [PesoUnitarioGramos], [Estado])
                    VALUES (N'250G', N'250 g', 250, 1);
                END;

                IF NOT EXISTS (SELECT 1 FROM [Inventario].[PresentacionesCafe] WHERE [Codigo] = N'454G')
                BEGIN
                    INSERT INTO [Inventario].[PresentacionesCafe] ([Codigo], [Nombre], [PesoUnitarioGramos], [Estado])
                    VALUES (N'454G', N'454 g', 454, 1);
                END;

                IF NOT EXISTS (SELECT 1 FROM [Inventario].[TiposCafe] WHERE [Codigo] = N'GRANO')
                BEGIN
                    INSERT INTO [Inventario].[TiposCafe] ([Codigo], [Nombre], [Estado])
                    VALUES (N'GRANO', N'Grano', 1);
                END;

                IF NOT EXISTS (SELECT 1 FROM [Inventario].[TiposCafe] WHERE [Codigo] = N'MOLIDO')
                BEGIN
                    INSERT INTO [Inventario].[TiposCafe] ([Codigo], [Nombre], [Estado])
                    VALUES (N'MOLIDO', N'Molido', 1);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosLoteCafe",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "LotesCafeExistencias",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "LotesCafeTerminado",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "PresentacionesCafe",
                schema: "Inventario");

            migrationBuilder.DropTable(
                name: "TiposCafe",
                schema: "Inventario");
        }
    }
}
