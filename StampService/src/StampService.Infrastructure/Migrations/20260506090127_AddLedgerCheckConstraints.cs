using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_stamp_transactions_amount_positive",
                table: "stamp_transactions",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stamp_transactions_transaction_type",
                table: "stamp_transactions",
                sql: "transaction_type IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_metric_balances_value_non_negative",
                table: "metric_balances",
                sql: "value >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stamp_transactions_amount_positive",
                table: "stamp_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stamp_transactions_transaction_type",
                table: "stamp_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_metric_balances_value_non_negative",
                table: "metric_balances");
        }
    }
}
