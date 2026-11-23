using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Marmitas.Infrastructure.Persistence;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261123140000_CompleteSpecificationSchema")]
    public partial class CompleteSpecificationSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS restaurant_users (
                    id UUID PRIMARY KEY,
                    restaurant_id UUID NOT NULL,
                    user_id UUID NOT NULL,
                    role VARCHAR(40) NOT NULL DEFAULT 'restaurant_staff',
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS restaurant_hours (
                    id UUID PRIMARY KEY,
                    restaurant_id UUID NOT NULL,
                    day_of_week INT NOT NULL,
                    open_time VARCHAR(20) NOT NULL DEFAULT '10:30',
                    close_time VARCHAR(20) NOT NULL DEFAULT '15:30',
                    is_open BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS delivery_zones (
                    id UUID PRIMARY KEY,
                    restaurant_id UUID NOT NULL,
                    neighborhood VARCHAR(120) NOT NULL,
                    city VARCHAR(120) NOT NULL DEFAULT 'São Paulo',
                    radius_km DOUBLE PRECISION NOT NULL DEFAULT 8.0,
                    delivery_fee NUMERIC(10,2) NOT NULL DEFAULT 6.90,
                    estimated_minutes INT NOT NULL DEFAULT 35,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS product_options (
                    id UUID PRIMARY KEY,
                    product_id UUID NOT NULL,
                    name VARCHAR(120) NOT NULL,
                    additional_price NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    max_quantity INT NOT NULL DEFAULT 1,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS subscription_items (
                    id UUID PRIMARY KEY,
                    subscription_id UUID NOT NULL,
                    product_id UUID NOT NULL,
                    product_name VARCHAR(150) NOT NULL DEFAULT '',
                    quantity INT NOT NULL DEFAULT 1,
                    day_of_week INT NOT NULL DEFAULT 1,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS subscription_deliveries (
                    id UUID PRIMARY KEY,
                    subscription_id UUID NOT NULL,
                    order_id UUID,
                    restaurant_id UUID,
                    address_id UUID,
                    scheduled_date TIMESTAMPTZ NOT NULL,
                    delivery_window VARCHAR(60) NOT NULL DEFAULT '11:30 - 13:30',
                    meals_count INT NOT NULL DEFAULT 1,
                    status VARCHAR(30) NOT NULL DEFAULT 'Scheduled',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS order_status_histories (
                    id UUID PRIMARY KEY,
                    order_id UUID NOT NULL,
                    from_status VARCHAR(40) NOT NULL,
                    to_status VARCHAR(40) NOT NULL,
                    changed_by_user_id UUID,
                    changed_by_role VARCHAR(40),
                    notes TEXT,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS driver_locations (
                    id UUID PRIMARY KEY,
                    courier_id UUID NOT NULL,
                    delivery_id UUID,
                    latitude DOUBLE PRECISION NOT NULL,
                    longitude DOUBLE PRECISION NOT NULL,
                    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS payment_transactions (
                    id UUID PRIMARY KEY,
                    payment_id UUID NOT NULL,
                    order_id UUID NOT NULL,
                    gateway VARCHAR(50) NOT NULL DEFAULT 'MercadoPago',
                    external_transaction_id VARCHAR(120) NOT NULL DEFAULT '',
                    amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    status VARCHAR(40) NOT NULL DEFAULT 'Approved',
                    raw_payload TEXT,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS refunds (
                    id UUID PRIMARY KEY,
                    payment_id UUID NOT NULL,
                    order_id UUID NOT NULL,
                    amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    reason VARCHAR(250) NOT NULL DEFAULT '',
                    status VARCHAR(40) NOT NULL DEFAULT 'Processed',
                    external_refund_id VARCHAR(120),
                    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS commissions (
                    id UUID PRIMARY KEY,
                    order_id UUID NOT NULL,
                    restaurant_id UUID NOT NULL,
                    order_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    rate_percentage NUMERIC(5,2) NOT NULL DEFAULT 10.00,
                    commission_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS financial_transactions (
                    id UUID PRIMARY KEY,
                    order_id UUID,
                    subscription_id UUID,
                    payout_id UUID,
                    target_type VARCHAR(40) NOT NULL DEFAULT 'Platform',
                    target_id UUID,
                    entry_type VARCHAR(20) NOT NULL DEFAULT 'Credit',
                    category VARCHAR(40) NOT NULL DEFAULT 'Revenue',
                    amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                    description VARCHAR(300) NOT NULL DEFAULT '',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS audit_logs (
                    id UUID PRIMARY KEY,
                    user_id UUID,
                    user_email VARCHAR(150),
                    ip_address VARCHAR(60),
                    action VARCHAR(80) NOT NULL,
                    entity_name VARCHAR(80) NOT NULL DEFAULT '',
                    entity_id VARCHAR(80) NOT NULL DEFAULT '',
                    old_values TEXT,
                    new_values TEXT,
                    correlation_id VARCHAR(100),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS refresh_tokens (
                    id UUID PRIMARY KEY,
                    user_id UUID NOT NULL,
                    token VARCHAR(200) NOT NULL,
                    expires_at TIMESTAMPTZ NOT NULL,
                    revoked_at TIMESTAMPTZ,
                    replaced_by_token VARCHAR(200),
                    created_by_ip VARCHAR(60),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS password_reset_tokens (
                    id UUID PRIMARY KEY,
                    user_id UUID NOT NULL,
                    email VARCHAR(180) NOT NULL,
                    code VARCHAR(20) NOT NULL,
                    expires_at TIMESTAMPTZ NOT NULL,
                    attempts INT NOT NULL DEFAULT 0,
                    used_at TIMESTAMPTZ,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS idempotency_records (
                    id UUID PRIMARY KEY,
                    key VARCHAR(120) NOT NULL,
                    request_path VARCHAR(200) NOT NULL,
                    user_id UUID,
                    status_code INT NOT NULL DEFAULT 200,
                    response_body TEXT NOT NULL DEFAULT '',
                    expires_at TIMESTAMPTZ NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS ix_restaurant_users_restaurant_id ON restaurant_users (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_restaurant_hours_restaurant_id ON restaurant_hours (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_delivery_zones_restaurant_id ON delivery_zones (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_product_options_product_id ON product_options (product_id);
                CREATE INDEX IF NOT EXISTS ix_subscription_deliveries_subscription_id ON subscription_deliveries (subscription_id);
                CREATE INDEX IF NOT EXISTS ix_order_status_histories_order_id ON order_status_histories (order_id);
                CREATE INDEX IF NOT EXISTS ix_driver_locations_courier_id ON driver_locations (courier_id);
                CREATE INDEX IF NOT EXISTS ix_financial_transactions_order_id ON financial_transactions (order_id);
                CREATE INDEX IF NOT EXISTS ix_audit_logs_entity_id ON audit_logs (entity_id);
                CREATE INDEX IF NOT EXISTS ix_idempotency_records_key_path ON idempotency_records (key, request_path);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "idempotency_records");
            migrationBuilder.DropTable(name: "password_reset_tokens");
            migrationBuilder.DropTable(name: "refresh_tokens");
            migrationBuilder.DropTable(name: "audit_logs");
            migrationBuilder.DropTable(name: "financial_transactions");
            migrationBuilder.DropTable(name: "commissions");
            migrationBuilder.DropTable(name: "refunds");
            migrationBuilder.DropTable(name: "payment_transactions");
            migrationBuilder.DropTable(name: "driver_locations");
            migrationBuilder.DropTable(name: "order_status_histories");
            migrationBuilder.DropTable(name: "subscription_deliveries");
            migrationBuilder.DropTable(name: "subscription_items");
            migrationBuilder.DropTable(name: "product_options");
            migrationBuilder.DropTable(name: "delivery_zones");
            migrationBuilder.DropTable(name: "restaurant_hours");
            migrationBuilder.DropTable(name: "restaurant_users");
        }
    }
}
