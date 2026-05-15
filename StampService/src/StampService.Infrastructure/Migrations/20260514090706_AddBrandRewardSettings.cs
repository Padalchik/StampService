using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandRewardSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_coins_enabled",
                table: "brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_metrics_enabled",
                table: "brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_coins_enabled",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "is_metrics_enabled",
                table: "brands");
        }
    }
}
