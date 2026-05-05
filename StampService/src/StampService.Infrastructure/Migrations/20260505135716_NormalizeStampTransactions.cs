using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStampTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stamp_transactions_loyalty_metric_definitions_metric_defini~",
                table: "stamp_transactions");

            migrationBuilder.DropIndex(
                name: "ix_stamp_transactions_metric_definition_id",
                table: "stamp_transactions");

            migrationBuilder.DropColumn(
                name: "metric_definition_id",
                table: "stamp_transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "metric_definition_id",
                table: "stamp_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE stamp_transactions AS st
                SET metric_definition_id = mb.metric_definition_id
                FROM metric_balances AS mb
                WHERE st.metric_balance_id = mb.id
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "metric_definition_id",
                table: "stamp_transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stamp_transactions_metric_definition_id",
                table: "stamp_transactions",
                column: "metric_definition_id");

            migrationBuilder.AddForeignKey(
                name: "FK_stamp_transactions_loyalty_metric_definitions_metric_defini~",
                table: "stamp_transactions",
                column: "metric_definition_id",
                principalTable: "loyalty_metric_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
