using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    public partial class CreatePlatformModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS addresses (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    street character varying(200) NOT NULL,
                    number character varying(30) NOT NULL,
                    complement character varying(150),
                    neighborhood character varying(100) NOT NULL,
                    city character varying(100) NOT NULL,
                    state character varying(50) NOT NULL,
                    zip_code character varying(20) NOT NULL,
                    is_main boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_addresses PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS favorites (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    restaurant_id uuid,
                    product_id uuid,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_favorites PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS notifications (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    title character varying(150) NOT NULL,
                    message character varying(500) NOT NULL,
                    is_read boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_notifications PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS notification_preferences (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    email_enabled boolean NOT NULL,
                    push_enabled boolean NOT NULL,
                    sms_enabled boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_notification_preferences PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS coupons (
                    id uuid NOT NULL,
                    code character varying(50) NOT NULL,
                    discount_percentage numeric(10,2) NOT NULL,
                    is_active boolean NOT NULL,
                    expires_at timestamp with time zone NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_coupons PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS coupon_usages (
                    id uuid NOT NULL,
                    coupon_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    used_at timestamp with time zone NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_coupon_usages PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS plans (
                    id uuid NOT NULL,
                    name character varying(150) NOT NULL,
                    description character varying(500) NOT NULL,
                    meals_per_week integer NOT NULL,
                    monthly_price numeric(10,2) NOT NULL,
                    is_active boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_plans PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS subscriptions (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    plan_id uuid NOT NULL,
                    plan_name character varying(150) NOT NULL,
                    monthly_price numeric(10,2) NOT NULL,
                    status character varying(30) NOT NULL,
                    start_date timestamp with time zone NOT NULL,
                    next_billing_date timestamp with time zone NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_subscriptions PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS payments (
                    id uuid NOT NULL,
                    order_id uuid NOT NULL,
                    amount numeric(10,2) NOT NULL,
                    method character varying(50) NOT NULL,
                    status character varying(30) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_payments PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS couriers (
                    id uuid NOT NULL,
                    user_id uuid,
                    name character varying(150) NOT NULL,
                    document character varying(30) NOT NULL,
                    phone character varying(30) NOT NULL,
                    status character varying(30) NOT NULL,
                    is_available boolean NOT NULL,
                    latitude double precision,
                    longitude double precision,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_couriers PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS courier_documents (
                    id uuid NOT NULL,
                    courier_id uuid NOT NULL,
                    type character varying(50) NOT NULL,
                    number character varying(50) NOT NULL,
                    status character varying(30) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_courier_documents PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS courier_vehicles (
                    id uuid NOT NULL,
                    courier_id uuid NOT NULL,
                    type character varying(50) NOT NULL,
                    model character varying(100) NOT NULL,
                    license_plate character varying(20) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_courier_vehicles PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS deliveries (
                    id uuid NOT NULL,
                    order_id uuid NOT NULL,
                    courier_id uuid,
                    status character varying(30) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_deliveries PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS reviews (
                    id uuid NOT NULL,
                    order_id uuid NOT NULL,
                    restaurant_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    rating integer NOT NULL,
                    comment character varying(500) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_reviews PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS payouts (
                    id uuid NOT NULL,
                    target_id uuid NOT NULL,
                    target_type character varying(30) NOT NULL,
                    gross_amount numeric(10,2) NOT NULL,
                    net_amount numeric(10,2) NOT NULL,
                    status character varying(30) NOT NULL,
                    processed_at timestamp with time zone,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_payouts PRIMARY KEY (id)
                );

                CREATE INDEX IF NOT EXISTS ix_addresses_user_id ON addresses (user_id);
                CREATE INDEX IF NOT EXISTS ix_favorites_user_id ON favorites (user_id);
                CREATE INDEX IF NOT EXISTS ix_notifications_user_id ON notifications (user_id);
                CREATE INDEX IF NOT EXISTS ix_subscriptions_user_id ON subscriptions (user_id);
                CREATE INDEX IF NOT EXISTS ix_payments_order_id ON payments (order_id);
                CREATE INDEX IF NOT EXISTS ix_deliveries_order_id ON deliveries (order_id);
                CREATE INDEX IF NOT EXISTS ix_reviews_order_id ON reviews (order_id);
                CREATE INDEX IF NOT EXISTS ix_reviews_restaurant_id ON reviews (restaurant_id);

                INSERT INTO roles (id, name, description, created_at, updated_at) VALUES
                    ('a1b2c3d4-0001-0000-0000-000000000001', 'customer', 'Cliente final', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('a1b2c3d4-0001-0000-0000-000000000002', 'restaurant_owner', 'Dono do restaurante', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('a1b2c3d4-0001-0000-0000-000000000003', 'restaurant_staff', 'Funcionário do restaurante', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('a1b2c3d4-0001-0000-0000-000000000004', 'driver', 'Entregador', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('a1b2c3d4-0001-0000-0000-000000000005', 'admin', 'Operação da plataforma', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('a1b2c3d4-0001-0000-0000-000000000006', 'super_admin', 'Configurações globais e financeiro', '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00')
                ON CONFLICT (name) DO NOTHING;

                ALTER TABLE product_categories ALTER COLUMN restaurant_id DROP NOT NULL;

                INSERT INTO product_categories (id, restaurant_id, name, display_order, created_at, updated_at) VALUES
                    ('00000000-0000-0000-0002-000000000001', NULL, 'Tradicionais', 1, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('00000000-0000-0000-0002-000000000002', NULL, 'Fitness & Low Carb', 2, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('00000000-0000-0000-0002-000000000003', NULL, 'Executiva', 3, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('00000000-0000-0000-0002-000000000004', NULL, 'Vegetariana & Vegana', 4, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                    ('00000000-0000-0000-0002-000000000005', NULL, 'Bebidas & Sobremesas', 5, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "payouts");
            migrationBuilder.DropTable(name: "reviews");
            migrationBuilder.DropTable(name: "deliveries");
            migrationBuilder.DropTable(name: "courier_vehicles");
            migrationBuilder.DropTable(name: "courier_documents");
            migrationBuilder.DropTable(name: "couriers");
            migrationBuilder.DropTable(name: "payments");
            migrationBuilder.DropTable(name: "subscriptions");
            migrationBuilder.DropTable(name: "plans");
            migrationBuilder.DropTable(name: "coupon_usages");
            migrationBuilder.DropTable(name: "coupons");
            migrationBuilder.DropTable(name: "notification_preferences");
            migrationBuilder.DropTable(name: "notifications");
            migrationBuilder.DropTable(name: "favorites");
            migrationBuilder.DropTable(name: "addresses");
        }
    }
}

