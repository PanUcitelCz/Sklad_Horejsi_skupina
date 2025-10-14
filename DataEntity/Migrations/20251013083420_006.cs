using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataEntity.Migrations
{
    /// <inheritdoc />
    public partial class _006 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Palety",
                columns: table => new
                {
                    PaletaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Typ = table.Column<int>(type: "int", nullable: false),
                    Stav = table.Column<int>(type: "int", nullable: false),
                    AdresaUlozeni = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Mnozstvi = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DatumVytvoreni = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Palety", x => x.PaletaId);
                    table.ForeignKey(
                        name: "FK_Palety_Materialy_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materialy",
                        principalColumn: "MaterialId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Palety_MaterialId",
                table: "Palety",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Palety");
        }
    }
}
