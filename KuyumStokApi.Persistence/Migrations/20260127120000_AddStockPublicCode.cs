using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260127120000_AddStockPublicCode")]
    public partial class AddStockPublicCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_code",
                table: "stocks",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_stocks_public_code",
                table: "stocks",
                column: "public_code",
                unique: true,
                filter: "public_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stocks_public_code",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "public_code",
                table: "stocks");
        }
    }
}
