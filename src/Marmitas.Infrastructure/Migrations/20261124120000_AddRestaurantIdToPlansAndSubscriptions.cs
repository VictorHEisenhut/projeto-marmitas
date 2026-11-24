using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Marmitas.Infrastructure.Persistence;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124120000_AddRestaurantIdToPlansAndSubscriptions")]
    public partial class AddRestaurantIdToPlansAndSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE plans ADD COLUMN IF NOT EXISTS restaurant_id UUID;
                ALTER TABLE plans ADD COLUMN IF NOT EXISTS restaurant_name VARCHAR(150) NOT NULL DEFAULT '';

                UPDATE plans
                SET restaurant_id = COALESCE((SELECT id FROM restaurants ORDER BY created_at LIMIT 1), 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa'),
                    restaurant_name = COALESCE((SELECT name FROM restaurants ORDER BY created_at LIMIT 1), 'Sabores da Vila Gastronomia')
                WHERE restaurant_id IS NULL OR restaurant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE plans p
                SET restaurant_name = r.name
                FROM restaurants r
                WHERE p.restaurant_id = r.id AND (p.restaurant_name IS NULL OR p.restaurant_name = '');

                ALTER TABLE plans ALTER COLUMN restaurant_id SET DEFAULT '00000000-0000-0000-0000-000000000000';
                ALTER TABLE plans ALTER COLUMN restaurant_id SET NOT NULL;

                ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS restaurant_id UUID;
                ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS restaurant_name VARCHAR(150) NOT NULL DEFAULT '';

                UPDATE subscriptions s
                SET restaurant_id = COALESCE(p.restaurant_id, s.preferred_restaurant_id, (SELECT id FROM restaurants ORDER BY created_at LIMIT 1), '00000000-0000-0000-0000-000000000000'),
                    restaurant_name = COALESCE(NULLIF(p.restaurant_name, ''), ''),
                    preferred_restaurant_id = COALESCE(s.preferred_restaurant_id, p.restaurant_id)
                FROM plans p
                WHERE s.plan_id = p.id AND (s.restaurant_id IS NULL OR s.restaurant_id = '00000000-0000-0000-0000-000000000000');

                UPDATE subscriptions
                SET restaurant_id = COALESCE(preferred_restaurant_id, (SELECT id FROM restaurants ORDER BY created_at LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE restaurant_id IS NULL;

                UPDATE subscriptions s
                SET restaurant_name = r.name
                FROM restaurants r
                WHERE s.restaurant_id = r.id AND (s.restaurant_name IS NULL OR s.restaurant_name = '');

                ALTER TABLE subscriptions ALTER COLUMN restaurant_id SET DEFAULT '00000000-0000-0000-0000-000000000000';
                ALTER TABLE subscriptions ALTER COLUMN restaurant_id SET NOT NULL;

                CREATE INDEX IF NOT EXISTS ix_plans_restaurant_id ON plans (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_subscriptions_restaurant_id ON subscriptions (restaurant_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_plans_restaurant_id;
                DROP INDEX IF EXISTS ix_subscriptions_restaurant_id;
                ALTER TABLE plans DROP COLUMN IF EXISTS restaurant_id;
                ALTER TABLE plans DROP COLUMN IF EXISTS restaurant_name;
                ALTER TABLE subscriptions DROP COLUMN IF EXISTS restaurant_id;
                ALTER TABLE subscriptions DROP COLUMN IF EXISTS restaurant_name;
            ");
        }
    }
}
