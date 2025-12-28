using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStockIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Önce yeni UUID kolonları oluştur (nullable, geçici)
            migrationBuilder.AddColumn<Guid>(
                name: "id_new",
                table: "stocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "stock_id_new",
                table: "sale_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "stock_id_new",
                table: "purchase_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "stock_id_new",
                table: "product_lifecycles",
                type: "uuid",
                nullable: true);

            // 2. Mevcut int ID'lere karşılık sequential GUID'ler üret ve yeni kolonlara yaz
            // PostgreSQL'de sequential GUID için uuid_generate_v1mc() kullanıyoruz (COMB-like)
            // Eğer extension yoksa, basit bir mapping tablosu oluşturup kullanıyoruz
            migrationBuilder.Sql(@"
                -- Mevcut stocks kayıtları için sequential GUID oluştur
                UPDATE stocks
                SET id_new = gen_random_uuid()
                WHERE id_new IS NULL;

                -- stock_id mapping için bir temp tablo oluştur (int -> uuid)
                CREATE TEMP TABLE stock_id_mapping (
                    old_id INTEGER,
                    new_id UUID
                );

                -- Mapping'i doldur
                INSERT INTO stock_id_mapping (old_id, new_id)
                SELECT id::integer, id_new
                FROM stocks;

                -- Foreign key kolonlarını güncelle
                UPDATE sale_details sd
                SET stock_id_new = sim.new_id
                FROM stock_id_mapping sim
                WHERE sd.stock_id = sim.old_id;

                UPDATE purchase_details pd
                SET stock_id_new = sim.new_id
                FROM stock_id_mapping sim
                WHERE pd.stock_id = sim.old_id;

                UPDATE product_lifecycles pl
                SET stock_id_new = sim.new_id
                FROM stock_id_mapping sim
                WHERE pl.stock_id = sim.old_id;

                -- Temp tabloyu temizle
                DROP TABLE stock_id_mapping;
            ");

            // 3. Eski kolonları kaldır
            migrationBuilder.DropForeignKey(
                name: "sale_details_stock_id_fkey",
                table: "sale_details");

            migrationBuilder.DropForeignKey(
                name: "purchase_details_stock_id_fkey",
                table: "purchase_details");

            migrationBuilder.DropForeignKey(
                name: "product_lifecycles_stock_id_fkey",
                table: "product_lifecycles");

            migrationBuilder.DropColumn(
                name: "id",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "stock_id",
                table: "sale_details");

            migrationBuilder.DropColumn(
                name: "stock_id",
                table: "purchase_details");

            migrationBuilder.DropColumn(
                name: "stock_id",
                table: "product_lifecycles");

            // 4. Yeni kolonları rename et
            migrationBuilder.RenameColumn(
                name: "id_new",
                table: "stocks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "stock_id_new",
                table: "sale_details",
                newName: "stock_id");

            migrationBuilder.RenameColumn(
                name: "stock_id_new",
                table: "purchase_details",
                newName: "stock_id");

            migrationBuilder.RenameColumn(
                name: "stock_id_new",
                table: "product_lifecycles",
                newName: "stock_id");

            // 5. Yeni kolonları NOT NULL yap ve primary key constraint'i ekle
            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "stocks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid?),
                oldType: "uuid",
                oldNullable: true);

            // Primary key constraint'i ekle
            migrationBuilder.AddPrimaryKey(
                name: "stocks_pkey",
                table: "stocks",
                column: "id");

            // 6. Foreign key constraint'leri yeniden ekle
            migrationBuilder.AddForeignKey(
                name: "sale_details_stock_id_fkey",
                table: "sale_details",
                column: "stock_id",
                principalTable: "stocks",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "purchase_details_stock_id_fkey",
                table: "purchase_details",
                column: "stock_id",
                principalTable: "stocks",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "product_lifecycles_stock_id_fkey",
                table: "product_lifecycles",
                column: "stock_id",
                principalTable: "stocks",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "stocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "stock_id",
                table: "sale_details",
                type: "integer",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "stock_id",
                table: "purchase_details",
                type: "integer",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "stock_id",
                table: "product_lifecycles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
