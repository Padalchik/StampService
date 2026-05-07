using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramBotFlow.Core.Data.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    roadmap = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bot_settings", x => x.id);
                });
        }
    }
}