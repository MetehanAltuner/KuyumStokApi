using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMilyemAndRawMilyemFromStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "milyem",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "raw_milyem",
                table: "stocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "milyem",
                table: "stocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "raw_milyem",
                table: "stocks",
                type: "integer",
                nullable: true,
                comment: "Ham milyem değeri");
        }
    }
}
