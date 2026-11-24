using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Seed;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (!db.Database.IsRelational())
            return;

        await db.Database.MigrateAsync();

        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE users ADD COLUMN IF NOT EXISTS mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS mfa_secret VARCHAR(100);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS failed_login_attempts INT NOT NULL DEFAULT 0;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS lockout_end_at TIMESTAMPTZ;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS terms_accepted_at TIMESTAMPTZ;

            ALTER TABLE addresses ADD COLUMN IF NOT EXISTS latitude DOUBLE PRECISION;
            ALTER TABLE addresses ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION;

            ALTER TABLE product_categories ALTER COLUMN restaurant_id DROP NOT NULL;
            INSERT INTO product_categories (id, restaurant_id, name, display_order, created_at, updated_at) VALUES
                ('00000000-0000-0000-0002-000000000001', NULL, 'Tradicionais', 1, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                ('00000000-0000-0000-0002-000000000002', NULL, 'Fitness & Low Carb', 2, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                ('00000000-0000-0000-0002-000000000003', NULL, 'Executiva', 3, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                ('00000000-0000-0000-0002-000000000004', NULL, 'Vegetariana & Vegana', 4, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00'),
                ('00000000-0000-0000-0002-000000000005', NULL, 'Bebidas & Sobremesas', 5, '2026-01-01 00:00:00+00', '2026-01-01 00:00:00+00')
            ON CONFLICT (id) DO NOTHING;

            DELETE FROM product_categories
            WHERE restaurant_id IS NOT NULL
              AND name IN ('Tradicionais', 'Fitness & Low Carb', 'Executiva', 'Vegetariana & Vegana', 'Bebidas & Sobremesas')
              AND id NOT IN (SELECT DISTINCT category_id FROM products);

            ALTER TABLE products ADD COLUMN IF NOT EXISTS promotional_price NUMERIC(10,2);
            ALTER TABLE products ADD COLUMN IF NOT EXISTS daily_stock INT;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS available_in_plans BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS allows_cutlery BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS cutlery_has_extra_cost BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS cutlery_price NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS addons_json TEXT NOT NULL DEFAULT '[]';

            CREATE TABLE IF NOT EXISTS product_options (
                id UUID PRIMARY KEY,
                product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
                name VARCHAR(120) NOT NULL,
                additional_price NUMERIC(10,2) NOT NULL DEFAULT 0.00,
                max_quantity INT NOT NULL DEFAULT 1,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_street VARCHAR(200) NOT NULL DEFAULT '';
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_number VARCHAR(40) NOT NULL DEFAULT '';
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_complement VARCHAR(120);
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_neighborhood VARCHAR(120) NOT NULL DEFAULT '';
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_city VARCHAR(120) NOT NULL DEFAULT '';
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_state VARCHAR(10) NOT NULL DEFAULT '';
            ALTER TABLE couriers ADD COLUMN IF NOT EXISTS billing_zip_code VARCHAR(30) NOT NULL DEFAULT '';

            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_street VARCHAR(200) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_number VARCHAR(40) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_complement VARCHAR(120);
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_neighborhood VARCHAR(120) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_city VARCHAR(120) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_state VARCHAR(10) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS address_zip_code VARCHAR(30) NOT NULL DEFAULT '';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS latitude DOUBLE PRECISION;
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION;
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS opening_hours VARCHAR(120) NOT NULL DEFAULT 'Seg a Sáb • 10:30 às 15:30';
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS delivery_fee NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS delivery_radius_km DOUBLE PRECISION NOT NULL DEFAULT 12.0;
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS bank_name VARCHAR(80);
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS bank_agency VARCHAR(30);
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS bank_account VARCHAR(40);
            ALTER TABLE restaurants ADD COLUMN IF NOT EXISTS pix_key VARCHAR(120);

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

            ALTER TABLE plans ADD COLUMN IF NOT EXISTS restaurant_id UUID;
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS restaurant_name VARCHAR(150) NOT NULL DEFAULT '';
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS meals_per_month INT NOT NULL DEFAULT 20;
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS frequency VARCHAR(30) NOT NULL DEFAULT 'Monthly';
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS allowed_days VARCHAR(100) NOT NULL DEFAULT 'Segunda a Sexta';
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS delivery_window VARCHAR(80) NOT NULL DEFAULT '11:00 às 14:30';
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS delivery_fee NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE plans ADD COLUMN IF NOT EXISTS cancellation_policy VARCHAR(300) NOT NULL DEFAULT 'Cancelamento livre até o fim do ciclo vigente.';

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
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS total_meals_in_cycle INT NOT NULL DEFAULT 20;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS used_meals_in_cycle INT NOT NULL DEFAULT 0;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS preferred_restaurant_id UUID;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS default_address_id UUID;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS failed_payment_attempts INT NOT NULL DEFAULT 0;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS grace_period_ends_at TIMESTAMPTZ;
            ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS cancelled_at TIMESTAMPTZ;

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

            ALTER TABLE orders ADD COLUMN IF NOT EXISTS subscription_id UUID;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS subtotal NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS delivery_fee NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS delivery_address TEXT;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS coupon_code VARCHAR(50);
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS discount_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS confirmation_pin VARCHAR(10) NOT NULL DEFAULT '1234';
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_message TEXT;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_created_at TIMESTAMPTZ;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_reply TEXT;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_replied_at TIMESTAMPTZ;

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

            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS distance_km DOUBLE PRECISION;
            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS delivery_fee NUMERIC(10,2) NOT NULL DEFAULT 8.50;
            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS confirmation_pin VARCHAR(10);
            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS assigned_at TIMESTAMPTZ;
            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS picked_up_at TIMESTAMPTZ;
            ALTER TABLE deliveries ADD COLUMN IF NOT EXISTS delivered_at TIMESTAMPTZ;

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

            ALTER TABLE payments ADD COLUMN IF NOT EXISTS gateway VARCHAR(50) NOT NULL DEFAULT 'MercadoPago';
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS external_transaction_id VARCHAR(120);
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS pix_qr_code TEXT;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS pix_copy_paste TEXT;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(120);
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS paid_at TIMESTAMPTZ;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS refunded_at TIMESTAMPTZ;

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

            CREATE TABLE IF NOT EXISTS user_consents (
                id UUID PRIMARY KEY,
                user_id UUID NOT NULL,
                purpose VARCHAR(80) NOT NULL DEFAULT 'TermsAndPrivacy',
                terms_version VARCHAR(30) NOT NULL DEFAULT 'v1.0',
                is_granted BOOLEAN NOT NULL DEFAULT TRUE,
                ip_address VARCHAR(60),
                consented_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS target_type VARCHAR(40) NOT NULL DEFAULT 'Customer';
            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS author_name VARCHAR(120) NOT NULL DEFAULT 'Cliente Verificado';
            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS restaurant_name VARCHAR(150) NOT NULL DEFAULT 'Restaurante Parceiro';

            UPDATE restaurants
            SET address_street = 'Rua Harmonia',
                address_number = '450',
                address_neighborhood = 'Vila Madalena',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '05435-000',
                latitude = -23.5535,
                longitude = -46.6902
            WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Av. Ibirapuera',
                address_number = '2120',
                address_neighborhood = 'Moema',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '04028-001',
                latitude = -23.6022,
                longitude = -46.6619
            WHERE id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Rua Treze de Maio',
                address_number = '890',
                address_neighborhood = 'Bela Vista',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '01327-000',
                latitude = -23.5594,
                longitude = -46.6468
            WHERE id = 'cccccccc-3333-3333-3333-cccccccccccc' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Rua dos Pinheiros',
                address_number = '315',
                address_neighborhood = 'Pinheiros',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '05422-010',
                latitude = -23.5661,
                longitude = -46.6805
            WHERE address_street = '';

            INSERT INTO addresses (id, user_id, street, number, complement, neighborhood, city, state, zip_code, is_main, latitude, longitude, created_at, updated_at)
            SELECT '44444444-aaaa-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'Rua Domingos de Morais', '1450', 'Apto 82', 'Vila Mariana', 'São Paulo', 'SP', '04010-200', TRUE, -23.5890, -46.6365, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '44444444-4444-4444-4444-444444444444')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO addresses (id, user_id, street, number, complement, neighborhood, city, state, zip_code, is_main, latitude, longitude, created_at, updated_at)
            SELECT '55555555-aaaa-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555', 'Rua Teodoro Sampaio', '980', 'Bloco B', 'Pinheiros', 'São Paulo', 'SP', '05406-050', TRUE, -23.5612, -46.6789, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '55555555-5555-5555-5555-555555555555')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO reviews (id, order_id, restaurant_id, user_id, rating, comment, target_type, author_name, restaurant_name, created_at, updated_at) VALUES
                ('77777777-0001-0001-0001-777777777771', '00000000-0000-0000-0000-000000000000', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', '44444444-4444-4444-4444-444444444444', 5, 'Marmita chegou super quentinha e muito bem temperada! A feijoada é nota 10.', 'Customer', 'Lucas Mendes', 'Sabores da Vila Gastronomia', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
                ('77777777-0001-0001-0001-777777777772', '00000000-0000-0000-0000-000000000000', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', '55555555-5555-5555-5555-555555555555', 5, 'Opções low carb deliciosas e porções na medida certa para a rotina da semana.', 'Customer', 'Beatriz Costa', 'FitLife Marmitaria & Nutrição', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
                ('77777777-0001-0001-0001-777777777773', '00000000-0000-0000-0000-000000000000', 'cccccccc-3333-3333-3333-cccccccccccc', '44444444-4444-4444-4444-444444444444', 4, 'Parmegiana executiva excelente e entrega muito rápida no horário do almoço.', 'Customer', 'Lucas Mendes', 'Cantina & Bistrô Paulista', NOW() - INTERVAL '5 hours', NOW() - INTERVAL '5 hours'),
                ('88888888-0002-0002-0002-888888888881', '00000000-0000-0000-0000-000000000000', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', '44444444-4444-4444-4444-444444444444', 5, 'Coleta super rápida na Sabores da Vila! Embalagem bem lacrada que não vaza no baú e equipe atenciosa com os motoboys.', 'Courier', 'Carlos Eduardo (Moto)', 'Sabores da Vila Gastronomia', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
                ('88888888-0002-0002-0002-888888888882', '00000000-0000-0000-0000-000000000000', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', '44444444-4444-4444-4444-444444444444', 5, 'Ótimo ponto de apoio para entregadores em Moema, pedido já estava pronto na bancada ao chegar.', 'Courier', 'Rafael Souza (Moto)', 'FitLife Marmitaria & Nutrição', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours'),
                ('88888888-0002-0002-0002-888888888883', '00000000-0000-0000-0000-000000000000', 'cccccccc-3333-3333-3333-cccccccccccc', '44444444-4444-4444-4444-444444444444', 4, 'Fácil estacionamento na Rua Treze de Maio para retirada rápida no horário de pico.', 'Courier', 'Marcos Vinicius (Bike)', 'Cantina & Bistrô Paulista', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO orders (id, customer_id, restaurant_id, subtotal, delivery_fee, total_amount, status, complaint_message, complaint_created_at, created_at, updated_at)
            SELECT '99999999-1111-1111-1111-999999999991', '44444444-4444-4444-4444-444444444444', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', 29.90, 0.00, 29.90, 'Delivered', 'Solicitei a farofa extra separada e veio junto na marmita.', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '1 day', NOW() - INTERVAL '4 hours'
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '44444444-4444-4444-4444-444444444444')
              AND EXISTS (SELECT 1 FROM restaurants WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plans (id, restaurant_id, restaurant_name, name, description, meals_per_week, meals_per_month, monthly_price, frequency, allowed_days, delivery_window, delivery_fee, cancellation_policy, is_active, created_at, updated_at)
            SELECT 'a1111111-1111-1111-1111-111111111101', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', 'Sabores da Vila Gastronomia', 'Caseiro 5x Semana', '5 marmitas tradicionais da semana com tempero caseiro e entrega gratuita.', 5, 20, 289.90, 'Monthly', 'Segunda a Sexta', '11:00 às 14:00', 0.00, 'Cancelamento livre até o fim do ciclo vigente.', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM restaurants WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa')
              AND NOT EXISTS (SELECT 1 FROM plans WHERE restaurant_id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa' AND is_active = TRUE)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plans (id, restaurant_id, restaurant_name, name, description, meals_per_week, meals_per_month, monthly_price, frequency, allowed_days, delivery_window, delivery_fee, cancellation_policy, is_active, created_at, updated_at)
            SELECT 'a1111111-1111-1111-1111-111111111102', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', 'Sabores da Vila Gastronomia', 'Caseiro Completo 10x', 'Almoço e jantar de segunda a sexta com pratos executivos e tradicionais.', 10, 40, 529.90, 'Monthly', 'Segunda a Sexta', '11:00 às 20:00', 0.00, 'Cancelamento livre até o fim do ciclo vigente.', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM restaurants WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa')
              AND (SELECT COUNT(*) FROM plans WHERE restaurant_id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa' AND is_active = TRUE) < 2
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plans (id, restaurant_id, restaurant_name, name, description, meals_per_week, meals_per_month, monthly_price, frequency, allowed_days, delivery_window, delivery_fee, cancellation_policy, is_active, created_at, updated_at)
            SELECT 'b2222222-2222-2222-2222-222222222201', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', 'FitLife Marmitaria & Nutrição', 'Fit Low Carb 5x', 'Marmitas balanceadas low carb com proteínas magras de segunda a sexta.', 5, 20, 319.90, 'Monthly', 'Segunda a Sexta', '11:00 às 14:30', 0.00, 'Cancelamento livre até o fim do ciclo vigente.', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM restaurants WHERE id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb')
              AND NOT EXISTS (SELECT 1 FROM plans WHERE restaurant_id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb' AND is_active = TRUE)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plans (id, restaurant_id, restaurant_name, name, description, meals_per_week, meals_per_month, monthly_price, frequency, allowed_days, delivery_window, delivery_fee, cancellation_policy, is_active, created_at, updated_at)
            SELECT 'b2222222-2222-2222-2222-222222222202', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', 'FitLife Marmitaria & Nutrição', 'Atleta 10x Semana', 'Plano de alta proteína para almoço e jantar durante toda a semana.', 10, 40, 579.90, 'Monthly', 'Segunda a Sábado', '10:30 às 20:30', 0.00, 'Cancelamento livre até o fim do ciclo vigente.', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM restaurants WHERE id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb')
              AND (SELECT COUNT(*) FROM plans WHERE restaurant_id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb' AND is_active = TRUE) < 2
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plans (id, restaurant_id, restaurant_name, name, description, meals_per_week, meals_per_month, monthly_price, frequency, allowed_days, delivery_window, delivery_fee, cancellation_policy, is_active, created_at, updated_at)
            SELECT 'c3333333-3333-3333-3333-333333333301', 'cccccccc-3333-3333-3333-cccccccccccc', 'Cantina & Bistrô Paulista', 'Executivo Semanal 5x', 'Pratos executivos selecionados da cantina entregues no horário comercial.', 5, 20, 349.90, 'Monthly', 'Segunda a Sexta', '11:30 às 14:30', 0.00, 'Cancelamento livre até o fim do ciclo vigente.', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM restaurants WHERE id = 'cccccccc-3333-3333-3333-cccccccccccc')
              AND NOT EXISTS (SELECT 1 FROM plans WHERE restaurant_id = 'cccccccc-3333-3333-3333-cccccccccccc' AND is_active = TRUE)
            ON CONFLICT (id) DO NOTHING;
        ");
    }
}
