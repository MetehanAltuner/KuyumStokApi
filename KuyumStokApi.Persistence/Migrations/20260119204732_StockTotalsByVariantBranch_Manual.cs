using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuyumStokApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockTotalsByVariantBranch_Manual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stocks_branch_id",
                table: "stocks");

            migrationBuilder.AddColumn<decimal>(
                name: "total_weight_gram",
                table: "stocks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_weight_gram",
                table: "sale_details",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_weight_gram",
                table: "purchase_details",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE stocks
                SET total_weight_gram = COALESCE(gram, 0) * COALESCE(quantity, 0)
                WHERE total_weight_gram = 0;

                CREATE TEMP TABLE stock_survivor_map AS
                SELECT
                    s.id AS old_id,
                    agg.survivor_id
                FROM stocks s
                JOIN (
                    SELECT MIN(id::text)::uuid AS survivor_id, branch_id, product_variant_id
                    FROM stocks
                    WHERE branch_id IS NOT NULL AND product_variant_id IS NOT NULL
                    GROUP BY branch_id, product_variant_id
                ) agg ON agg.branch_id = s.branch_id AND agg.product_variant_id = s.product_variant_id;

                UPDATE stocks s
                SET quantity = agg.total_qty,
                    total_weight_gram = agg.total_weight_gram,
                    updated_at = COALESCE(s.updated_at, CURRENT_TIMESTAMP)
                FROM (
                    SELECT MIN(id::text)::uuid AS survivor_id,
                           branch_id,
                           product_variant_id,
                           SUM(COALESCE(quantity, 0)) AS total_qty,
                           SUM(COALESCE(total_weight_gram, 0)) AS total_weight_gram
                    FROM stocks
                    WHERE branch_id IS NOT NULL AND product_variant_id IS NOT NULL
                    GROUP BY branch_id, product_variant_id
                ) agg
                WHERE s.id = agg.survivor_id;

                UPDATE sale_details sd
                SET stock_id = map.survivor_id
                FROM stock_survivor_map map
                WHERE sd.stock_id = map.old_id
                  AND map.old_id <> map.survivor_id;

                UPDATE purchase_details pd
                SET stock_id = map.survivor_id
                FROM stock_survivor_map map
                WHERE pd.stock_id = map.old_id
                  AND map.old_id <> map.survivor_id;

                UPDATE product_lifecycles pl
                SET stock_id = map.survivor_id
                FROM stock_survivor_map map
                WHERE pl.stock_id = map.old_id
                  AND map.old_id <> map.survivor_id;

                DELETE FROM stocks s
                USING stock_survivor_map map
                WHERE s.id = map.old_id
                  AND map.old_id <> map.survivor_id;

                DROP TABLE stock_survivor_map;

                UPDATE sale_details sd
                SET total_weight_gram = COALESCE(st.gram, 0) * COALESCE(sd.quantity, 0)
                FROM stocks st
                WHERE sd.stock_id = st.id
                  AND sd.total_weight_gram = 0;

                UPDATE purchase_details pd
                SET total_weight_gram = COALESCE(st.gram, 0) * COALESCE(pd.quantity, 0)
                FROM stocks st
                WHERE pd.stock_id = st.id
                  AND pd.total_weight_gram = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "ux_stocks_branch_variant",
                table: "stocks",
                columns: new[] { "branch_id", "product_variant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stocks_branch_variant",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "total_weight_gram",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "total_weight_gram",
                table: "sale_details");

            migrationBuilder.DropColumn(
                name: "total_weight_gram",
                table: "purchase_details");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_branch_id",
                table: "stocks",
                column: "branch_id");
        }
    }
}
