using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Marmitas.Infrastructure.Persistence;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124180000_AddRestaurantIntegrations")]
    public partial class AddRestaurantIntegrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS restaurant_integrations (
                    id UUID PRIMARY KEY,
                    restaurant_id UUID NOT NULL,
                    provider VARCHAR(40) NOT NULL,
                    access_token TEXT NOT NULL DEFAULT '',
                    public_key TEXT NOT NULL DEFAULT '',
                    enabled BOOLEAN NOT NULL DEFAULT FALSE,
                    account_id VARCHAR(80),
                    account_email VARCHAR(200),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_restaurant_integrations_restaurant_provider
                    ON restaurant_integrations (restaurant_id, provider);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_restaurant_integrations_restaurant_provider;
                DROP TABLE IF EXISTS restaurant_integrations;
            ");
        }
    }
}
