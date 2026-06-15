using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneAuthSmsSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phone_auth_sms_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_auth_sms_settings", x => x.id);
                    table.CheckConstraint("ck_phone_auth_sms_settings_singleton", "id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phone_auth_sms_settings");
        }
    }
}
