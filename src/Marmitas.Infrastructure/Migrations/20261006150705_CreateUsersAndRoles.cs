using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Marmitas.Infrastructure.Migrations
{

    public partial class CreateUsersAndRoles : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Consultar pedidos", "orders.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Criar pedidos", "orders.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Atualizar status de pedidos", "orders.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cancelar pedidos", "orders.cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Consultar pagamentos", "payments.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reembolsar pagamentos", "payments.refund", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Aprovar restaurantes", "restaurants.approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Aprovar entregadores", "drivers.approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Consultar financeiro", "financial.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Executar repasses", "financial.payout", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cliente final", "customer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dono do restaurante", "restaurant_owner", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Funcion�rio do restaurante", "restaurant_staff", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entregador", "driver", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Opera��o da plataforma", "admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Configura��es globais e financeiro", "super_admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000001") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000002"), new Guid("a1b2c3d4-0001-0000-0000-000000000001") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000004"), new Guid("a1b2c3d4-0001-0000-0000-000000000001") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000002") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000003"), new Guid("a1b2c3d4-0001-0000-0000-000000000002") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000003") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000003"), new Guid("a1b2c3d4-0001-0000-0000-000000000003") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000004") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000003"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000004"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000005"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000006"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000007"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000008"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000009"), new Guid("a1b2c3d4-0001-0000-0000-000000000005") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000001"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000002"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000003"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000004"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000005"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000006"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000007"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000008"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000009"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000010"), new Guid("a1b2c3d4-0001-0000-0000-000000000006") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
