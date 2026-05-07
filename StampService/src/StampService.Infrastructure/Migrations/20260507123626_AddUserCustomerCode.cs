using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCustomerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "users",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF (SELECT COUNT(*) FROM users) > 10000 THEN
                        RAISE EXCEPTION 'Cannot backfill 4-digit customer codes for more than 10000 users';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                WITH numbered_users AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY created_at, id) - 1 AS number
                    FROM users
                )
                UPDATE users
                SET customer_code = LPAD(numbered_users.number::text, 4, '0')
                FROM numbered_users
                WHERE users.id = numbered_users.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "customer_code",
                table: "users",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_customer_code",
                table: "users",
                column: "customer_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_customer_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "users");
        }
    }
}
