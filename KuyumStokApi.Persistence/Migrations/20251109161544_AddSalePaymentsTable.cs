using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePaymentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalePayments_banks_BankId",
                table: "SalePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SalePayments_payment_methods_PaymentMethodId",
                table: "SalePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SalePayments_sales_SaleId",
                table: "SalePayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalePayments",
                table: "SalePayments");

            migrationBuilder.RenameTable(
                name: "SalePayments",
                newName: "sale_payments");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "sale_payments",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sale_payments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "sale_payments",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SaleId",
                table: "sale_payments",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "sale_payments",
                newName: "payment_method_id");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "sale_payments",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sale_payments",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CommissionRate",
                table: "sale_payments",
                newName: "commission_rate");

            migrationBuilder.RenameColumn(
                name: "BankId",
                table: "sale_payments",
                newName: "bank_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalePayments_SaleId",
                table: "sale_payments",
                newName: "IX_sale_payments_sale_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalePayments_PaymentMethodId",
                table: "sale_payments",
                newName: "IX_sale_payments_payment_method_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalePayments_BankId",
                table: "sale_payments",
                newName: "IX_sale_payments_bank_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "sale_payments",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "sale_payments",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "sale_payments_pkey",
                table: "sale_payments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "sale_payments_bank_id_fkey",
                table: "sale_payments",
                column: "bank_id",
                principalTable: "banks",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "sale_payments_payment_method_id_fkey",
                table: "sale_payments",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "sale_payments_sale_id_fkey",
                table: "sale_payments",
                column: "sale_id",
                principalTable: "sales",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "sale_payments_bank_id_fkey",
                table: "sale_payments");

            migrationBuilder.DropForeignKey(
                name: "sale_payments_payment_method_id_fkey",
                table: "sale_payments");

            migrationBuilder.DropForeignKey(
                name: "sale_payments_sale_id_fkey",
                table: "sale_payments");

            migrationBuilder.DropPrimaryKey(
                name: "sale_payments_pkey",
                table: "sale_payments");

            migrationBuilder.RenameTable(
                name: "sale_payments",
                newName: "SalePayments");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "SalePayments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SalePayments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "SalePayments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                table: "SalePayments",
                newName: "SaleId");

            migrationBuilder.RenameColumn(
                name: "payment_method_id",
                table: "SalePayments",
                newName: "PaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "SalePayments",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SalePayments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "commission_rate",
                table: "SalePayments",
                newName: "CommissionRate");

            migrationBuilder.RenameColumn(
                name: "bank_id",
                table: "SalePayments",
                newName: "BankId");

            migrationBuilder.RenameIndex(
                name: "IX_sale_payments_sale_id",
                table: "SalePayments",
                newName: "IX_SalePayments_SaleId");

            migrationBuilder.RenameIndex(
                name: "IX_sale_payments_payment_method_id",
                table: "SalePayments",
                newName: "IX_SalePayments_PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_sale_payments_bank_id",
                table: "SalePayments",
                newName: "IX_SalePayments_BankId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SalePayments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SalePayments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalePayments",
                table: "SalePayments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalePayments_banks_BankId",
                table: "SalePayments",
                column: "BankId",
                principalTable: "banks",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalePayments_payment_methods_PaymentMethodId",
                table: "SalePayments",
                column: "PaymentMethodId",
                principalTable: "payment_methods",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalePayments_sales_SaleId",
                table: "SalePayments",
                column: "SaleId",
                principalTable: "sales",
                principalColumn: "id");
        }
    }
}
