using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedReceiptAndFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "stocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_favorite",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "customers",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_national_id",
                table: "customers",
                column: "national_id",
                unique: true,
                filter: "national_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_customers_national_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "color",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "is_favorite",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "customers");
        }
    }
}
