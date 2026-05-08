using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coin_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_wallets", x => x.id);
                    table.CheckConstraint("ck_coin_wallets_value_non_negative", "value >= 0");
                    table.ForeignKey(
                        name: "fk_coin_wallets_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_coin_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coin_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coin_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_transactions", x => x.id);
                    table.CheckConstraint("ck_coin_transactions_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_coin_transactions_transaction_type", "transaction_type IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_coin_transactions_coin_wallets_coin_wallet_id",
                        column: x => x.coin_wallet_id,
                        principalTable: "coin_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coin_transactions_coin_wallet_id",
                table: "coin_transactions",
                column: "coin_wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_coin_wallets_brand_id",
                table: "coin_wallets",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_coin_wallets_user_id_brand_id",
                table: "coin_wallets",
                columns: new[] { "user_id", "brand_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coin_transactions");

            migrationBuilder.DropTable(
                name: "coin_wallets");
        }
    }
}
