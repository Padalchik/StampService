using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMetricCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_loyalty_metric_definitions_brand_id_code",
                table: "loyalty_metric_definitions");

            migrationBuilder.DropColumn(
                name: "code",
                table: "loyalty_metric_definitions");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_metric_definitions_brand_id",
                table: "loyalty_metric_definitions",
                column: "brand_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_loyalty_metric_definitions_brand_id",
                table: "loyalty_metric_definitions");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "loyalty_metric_definitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_metric_definitions_brand_id_code",
                table: "loyalty_metric_definitions",
                columns: new[] { "brand_id", "code" },
                unique: true);
        }
    }
}
