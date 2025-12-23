using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawMilyemWorkmanshipMilyemToStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "raw_milyem",
                table: "stocks",
                type: "integer",
                nullable: true,
                comment: "Ham milyem değeri");

            migrationBuilder.AddColumn<int>(
                name: "workmanship_milyem",
                table: "stocks",
                type: "integer",
                nullable: true,
                comment: "İşçilik milyem değeri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "raw_milyem",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "workmanship_milyem",
                table: "stocks");
        }
    }
}
