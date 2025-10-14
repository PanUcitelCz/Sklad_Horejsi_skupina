using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataEntity.Migrations
{
    /// <inheritdoc />
    public partial class _004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materialy_MernaJednotka_MernaJednotkaId",
                table: "Materialy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MernaJednotka",
                table: "MernaJednotka");

            migrationBuilder.RenameTable(
                name: "MernaJednotka",
                newName: "MerneJednotky");

            migrationBuilder.AlterColumn<string>(
                name: "Komentar",
                table: "Materialy",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MerneJednotky",
                table: "MerneJednotky",
                column: "MernaJednotkaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materialy_MerneJednotky_MernaJednotkaId",
                table: "Materialy",
                column: "MernaJednotkaId",
                principalTable: "MerneJednotky",
                principalColumn: "MernaJednotkaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materialy_MerneJednotky_MernaJednotkaId",
                table: "Materialy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MerneJednotky",
                table: "MerneJednotky");

            migrationBuilder.RenameTable(
                name: "MerneJednotky",
                newName: "MernaJednotka");

            migrationBuilder.AlterColumn<string>(
                name: "Komentar",
                table: "Materialy",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MernaJednotka",
                table: "MernaJednotka",
                column: "MernaJednotkaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materialy_MernaJednotka_MernaJednotkaId",
                table: "Materialy",
                column: "MernaJednotkaId",
                principalTable: "MernaJednotka",
                principalColumn: "MernaJednotkaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
