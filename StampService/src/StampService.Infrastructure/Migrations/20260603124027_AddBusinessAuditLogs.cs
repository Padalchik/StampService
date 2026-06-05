using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operation_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    target_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<int>(type: "integer", nullable: true),
                    balance_before = table.Column<int>(type: "integer", nullable: true),
                    balance_after = table.Column<int>(type: "integer", nullable: true),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_audit_logs", x => x.id);
                    table.CheckConstraint("ck_business_audit_logs_operation_status", "operation_status IN ('Succeeded', 'Rejected', 'Failed')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_audit_logs_actor_user_id_occurred_at",
                table: "business_audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_business_audit_logs_brand_id_occurred_at",
                table: "business_audit_logs",
                columns: new[] { "brand_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_business_audit_logs_customer_user_id_occurred_at",
                table: "business_audit_logs",
                columns: new[] { "customer_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_business_audit_logs_occurred_at",
                table: "business_audit_logs",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_business_audit_logs_operation_type_operation_status_occurre~",
                table: "business_audit_logs",
                columns: new[] { "operation_type", "operation_status", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_audit_logs");
        }
    }
}
