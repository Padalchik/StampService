using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    house_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_locations_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_metric_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_metric_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_loyalty_metric_definitions_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "brand_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_brand_memberships_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_brand_memberships_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_brand_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_type = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_identities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_balances_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_metric_balances_loyalty_metric_definitions_metric_definitio~",
                        column: x => x.metric_definition_id,
                        principalTable: "loyalty_metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metric_balances_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stamp_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stamp_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_stamp_transactions_loyalty_metric_definitions_metric_defini~",
                        column: x => x.metric_definition_id,
                        principalTable: "loyalty_metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stamp_transactions_metric_balances_metric_balance_id",
                        column: x => x.metric_balance_id,
                        principalTable: "metric_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brand_memberships_brand_id",
                table: "brand_memberships",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_brand_memberships_role_id",
                table: "brand_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_brand_memberships_user_id_brand_id",
                table: "brand_memberships",
                columns: new[] { "user_id", "brand_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locations_brand_id",
                table: "locations",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_metric_definitions_brand_id_code",
                table: "loyalty_metric_definitions",
                columns: new[] { "brand_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metric_balances_brand_id",
                table: "metric_balances",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_balances_metric_definition_id",
                table: "metric_balances",
                column: "metric_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_metric_balances_user_id_brand_id_metric_definition_id",
                table: "metric_balances",
                columns: new[] { "user_id", "brand_id", "metric_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_system_name",
                table: "roles",
                column: "system_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stamp_transactions_metric_balance_id",
                table: "stamp_transactions",
                column: "metric_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_stamp_transactions_metric_definition_id",
                table: "stamp_transactions",
                column: "metric_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_identities_type_key",
                table: "user_identities",
                columns: new[] { "identity_type", "key" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_identities_user_id",
                table: "user_identities",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_memberships");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "stamp_transactions");

            migrationBuilder.DropTable(
                name: "user_identities");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "metric_balances");

            migrationBuilder.DropTable(
                name: "loyalty_metric_definitions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "brands");
        }
    }
}
