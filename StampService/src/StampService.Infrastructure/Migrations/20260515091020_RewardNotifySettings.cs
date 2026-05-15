using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RewardNotifySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_digest_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    message_to_user_interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    scan_interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    batch_size = table.Column<int>(type: "integer", nullable: false),
                    max_brands_per_message = table.Column<int>(type: "integer", nullable: false),
                    max_rewards_per_brand = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_digest_settings", x => x.id);
                    table.CheckConstraint("ck_reward_digest_settings_batch_size_positive", "batch_size > 0");
                    table.CheckConstraint("ck_reward_digest_settings_max_brands_positive", "max_brands_per_message > 0");
                    table.CheckConstraint("ck_reward_digest_settings_max_rewards_positive", "max_rewards_per_brand > 0");
                    table.CheckConstraint("ck_reward_digest_settings_message_interval_positive", "message_to_user_interval_minutes > 0");
                    table.CheckConstraint("ck_reward_digest_settings_scan_interval_positive", "scan_interval_minutes > 0");
                    table.CheckConstraint("ck_reward_digest_settings_singleton", "id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reward_digest_settings");
        }
    }
}
