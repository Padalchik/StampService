using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeMetricRewardAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_welcome_metric_rewards",
                columns: table => new
                {
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_welcome_metric_rewards", x => new { x.brand_id, x.metric_definition_id });
                    table.ForeignKey(
                        name: "FK_brand_welcome_metric_rewards_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO brand_welcome_metric_rewards (brand_id, metric_definition_id, amount)
                SELECT id, unnest(welcome_metric_definition_ids), 1
                FROM brands
                WHERE cardinality(welcome_metric_definition_ids) > 0
                """);

            migrationBuilder.DropColumn(
                name: "welcome_metric_definition_ids",
                table: "brands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid[]>(
                name: "welcome_metric_definition_ids",
                table: "brands",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'::uuid[]");

            migrationBuilder.Sql(
                """
                UPDATE brands
                SET welcome_metric_definition_ids = rewards.metric_definition_ids
                FROM (
                    SELECT brand_id, array_agg(metric_definition_id ORDER BY metric_definition_id) AS metric_definition_ids
                    FROM brand_welcome_metric_rewards
                    GROUP BY brand_id
                ) rewards
                WHERE brands.id = rewards.brand_id
                """);

            migrationBuilder.DropTable(
                name: "brand_welcome_metric_rewards");
        }
    }
}
