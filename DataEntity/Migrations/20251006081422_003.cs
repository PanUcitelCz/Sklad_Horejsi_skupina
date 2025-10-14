using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataEntity.Migrations
{
    /// <inheritdoc />
    public partial class _003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MernaJednotkaId",
                table: "Materialy",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MernaJednotka",
                columns: table => new
                {
                    MernaJednotkaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Popis = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MernaJednotka", x => x.MernaJednotkaId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materialy_MernaJednotkaId",
                table: "Materialy",
                column: "MernaJednotkaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materialy_MernaJednotka_MernaJednotkaId",
                table: "Materialy",
                column: "MernaJednotkaId",
                principalTable: "MernaJednotka",
                principalColumn: "MernaJednotkaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materialy_MernaJednotka_MernaJednotkaId",
                table: "Materialy");

            migrationBuilder.DropTable(
                name: "MernaJednotka");

            migrationBuilder.DropIndex(
                name: "IX_Materialy_MernaJednotkaId",
                table: "Materialy");

            migrationBuilder.DropColumn(
                name: "MernaJednotkaId",
                table: "Materialy");
        }
    }
}
