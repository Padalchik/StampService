using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandWelcomeRewardSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_welcome_rewards_enabled",
                table: "brands",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "welcome_coins_amount",
                table: "brands",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid[]>(
                name: "welcome_metric_definition_ids",
                table: "brands",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'::uuid[]");

            migrationBuilder.AddColumn<string>(
                name: "welcome_reward_comment",
                table: "brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Приветственная награда");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_welcome_rewards_enabled",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "welcome_coins_amount",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "welcome_metric_definition_ids",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "welcome_reward_comment",
                table: "brands");
        }
    }
}
