using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePaymentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sale_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_id = table.Column<int>(type: "integer", nullable: true),
                    payment_method_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("sale_payments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "sale_payments_bank_id_fkey",
                        column: x => x.bank_id,
                        principalTable: "banks",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sale_payments_payment_method_id_fkey",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sale_payments_sale_id_fkey",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_bank_id",
                table: "sale_payments",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_payment_method_id",
                table: "sale_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_sale_id",
                table: "sale_payments",
                column: "sale_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_payments");
        }
    }
}
