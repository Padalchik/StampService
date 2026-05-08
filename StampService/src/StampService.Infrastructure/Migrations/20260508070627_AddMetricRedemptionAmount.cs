using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricRedemptionAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "redemption_amount",
                table: "loyalty_metric_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_loyalty_metric_definitions_redemption_amount_positive",
                table: "loyalty_metric_definitions",
                sql: "redemption_amount > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_loyalty_metric_definitions_redemption_amount_positive",
                table: "loyalty_metric_definitions");

            migrationBuilder.DropColumn(
                name: "redemption_amount",
                table: "loyalty_metric_definitions");
        }
    }
}
