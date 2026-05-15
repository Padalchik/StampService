using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotifyUsersForPresent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_digest_states",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_digest_sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_wallet_opened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_digest_states", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_customer_digest_states_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_digest_states_last_digest_sent_at_utc",
                table: "customer_digest_states",
                column: "last_digest_sent_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_customer_digest_states_last_wallet_opened_at_utc",
                table: "customer_digest_states",
                column: "last_wallet_opened_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_digest_states");
        }
    }
}
