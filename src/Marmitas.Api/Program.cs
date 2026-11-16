using Marmitas.Api.Extensions;
using Marmitas.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<Marmitas.Infrastructure.Persistence.AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(Marmitas.Infrastructure.Persistence.AppDbContext).Assembly.FullName))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IUserRepository, Marmitas.Infrastructure.Persistence.Repositories.UserRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IRestaurantRepository, Marmitas.Infrastructure.Persistence.Repositories.RestaurantRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IOrderRepository, Marmitas.Infrastructure.Persistence.Repositories.OrderRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.ICustomerProfileRepository, Marmitas.Infrastructure.Persistence.Repositories.CustomerProfileRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.ISubscriptionRepository, Marmitas.Infrastructure.Persistence.Repositories.SubscriptionRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IDeliveryRepository, Marmitas.Infrastructure.Persistence.Repositories.DeliveryRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IPaymentRepository, Marmitas.Infrastructure.Persistence.Repositories.PaymentRepository>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.IPasswordHasher, Marmitas.Infrastructure.Identity.BCryptPasswordHasher>();
builder.Services.AddScoped<Marmitas.Application.Common.Interfaces.ITokenService, Marmitas.Infrastructure.Identity.JwtTokenService>();

builder.Services.AddScoped<Marmitas.Application.Users.IRegisterUserService, Marmitas.Application.Users.RegisterUserService>();
builder.Services.AddScoped<Marmitas.Application.Users.ILoginService, Marmitas.Application.Users.LoginService>();
builder.Services.AddScoped<Marmitas.Application.Restaurants.Queries.IRestaurantQueryService, Marmitas.Application.Restaurants.Queries.RestaurantQueryService>();
builder.Services.AddScoped<Marmitas.Application.Orders.ICreateOrderService, Marmitas.Application.Orders.CreateOrderService>();
builder.Services.AddHttpClient<Marmitas.Application.Common.Interfaces.IAiAssistantService, Marmitas.Infrastructure.Ai.GeminiAiAssistantService>();

builder.Services
    .AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    })
    .AddApiControllers()
    .AddSwaggerConfig()
    .AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Marmitas.Infrastructure.Persistence.AppDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(@"
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

            ALTER TABLE products ADD COLUMN IF NOT EXISTS available_in_plans BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS allows_cutlery BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS cutlery_has_extra_cost BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS cutlery_price NUMERIC(10,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE products ADD COLUMN IF NOT EXISTS addons_json TEXT NOT NULL DEFAULT '[]';

            CREATE TABLE IF NOT EXISTS product_addons (
                id UUID PRIMARY KEY,
                product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
                name VARCHAR(120) NOT NULL,
                extra_price NUMERIC(10,2) NOT NULL DEFAULT 0.00 CHECK (extra_price >= 0),
                max_quantity INT NOT NULL DEFAULT 1 CHECK (max_quantity >= 1),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
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

            UPDATE restaurants
            SET address_street = 'Rua Harmonia',
                address_number = '450',
                address_neighborhood = 'Vila Madalena',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '05435-000'
            WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Av. Ibirapuera',
                address_number = '2120',
                address_neighborhood = 'Moema',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '04028-001'
            WHERE id = 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Rua Treze de Maio',
                address_number = '890',
                address_neighborhood = 'Bela Vista',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '01327-000'
            WHERE id = 'cccccccc-3333-3333-3333-cccccccccccc' AND address_street = '';

            UPDATE restaurants
            SET address_street = 'Rua dos Pinheiros',
                address_number = '315',
                address_neighborhood = 'Pinheiros',
                address_city = 'São Paulo',
                address_state = 'SP',
                address_zip_code = '05422-010'
            WHERE address_street = '';

            INSERT INTO addresses (id, user_id, street, number, complement, neighborhood, city, state, zip_code, is_main, created_at, updated_at)
            SELECT '44444444-aaaa-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'Rua Domingos de Morais', '1450', 'Apto 82', 'Vila Mariana', 'São Paulo', 'SP', '04010-200', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '44444444-4444-4444-4444-444444444444')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO addresses (id, user_id, street, number, complement, neighborhood, city, state, zip_code, is_main, created_at, updated_at)
            SELECT '55555555-aaaa-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555', 'Rua Teodoro Sampaio', '980', 'Bloco B', 'Pinheiros', 'São Paulo', 'SP', '05406-050', TRUE, NOW(), NOW()
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '55555555-5555-5555-5555-555555555555')
            ON CONFLICT (id) DO NOTHING;

            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_message TEXT;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_created_at TIMESTAMPTZ;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_reply TEXT;
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS complaint_replied_at TIMESTAMPTZ;

            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS target_type VARCHAR(40) NOT NULL DEFAULT 'Customer';
            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS author_name VARCHAR(120) NOT NULL DEFAULT 'Cliente Verificado';
            ALTER TABLE reviews ADD COLUMN IF NOT EXISTS restaurant_name VARCHAR(150) NOT NULL DEFAULT 'Restaurante Parceiro';

            INSERT INTO reviews (id, order_id, restaurant_id, user_id, rating, comment, target_type, author_name, restaurant_name, created_at, updated_at) VALUES
                ('77777777-0001-0001-0001-777777777771', '00000000-0000-0000-0000-000000000000', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', '44444444-4444-4444-4444-444444444444', 5, 'Marmita chegou super quentinha e muito bem temperada! A feijoada é nota 10.', 'Customer', 'Lucas Mendes', 'Sabores da Vila Gastronomia', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
                ('77777777-0001-0001-0001-777777777772', '00000000-0000-0000-0000-000000000000', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', '55555555-5555-5555-5555-555555555555', 5, 'Opções low carb deliciosas e porções na medida certa para a rotina da semana.', 'Customer', 'Beatriz Costa', 'FitLife Marmitaria & Nutrição', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
                ('77777777-0001-0001-0001-777777777773', '00000000-0000-0000-0000-000000000000', 'cccccccc-3333-3333-3333-cccccccccccc', '44444444-4444-4444-4444-444444444444', 4, 'Parmegiana executiva excelente e entrega muito rápida no horário do almoço.', 'Customer', 'Lucas Mendes', 'Cantina & Bistrô Paulista', NOW() - INTERVAL '5 hours', NOW() - INTERVAL '5 hours'),
                ('88888888-0002-0002-0002-888888888881', '00000000-0000-0000-0000-000000000000', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', '44444444-4444-4444-4444-444444444444', 5, 'Coleta super rápida na Sabores da Vila! Embalagem bem lacrada que não vaza no baú e equipe atenciosa com os motoboys.', 'Courier', 'Carlos Eduardo (Moto)', 'Sabores da Vila Gastronomia', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
                ('88888888-0002-0002-0002-888888888882', '00000000-0000-0000-0000-000000000000', 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb', '44444444-4444-4444-4444-444444444444', 5, 'Ótimo ponto de apoio para entregadores em Moema, pedido já estava pronto na bancada ao chegar.', 'Courier', 'Rafael Souza (Moto)', 'FitLife Marmitaria & Nutrição', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours'),
                ('88888888-0002-0002-0002-888888888883', '00000000-0000-0000-0000-000000000000', 'cccccccc-3333-3333-3333-cccccccccccc', '44444444-4444-4444-4444-444444444444', 4, 'Fácil estacionamento na Rua Treze de Maio para retirada rápida no horário de pico.', 'Courier', 'Marcos Vinicius (Bike)', 'Cantina & Bistrô Paulista', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO orders (id, customer_id, restaurant_id, total_amount, status, complaint_message, complaint_created_at, created_at, updated_at)
            SELECT '99999999-1111-1111-1111-999999999991', '44444444-4444-4444-4444-444444444444', 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa', 29.90, 'Delivered', 'Solicitei a farofa extra separada e veio junto na marmita.', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '1 day', NOW() - INTERVAL '4 hours'
            WHERE EXISTS (SELECT 1 FROM users WHERE id = '44444444-4444-4444-4444-444444444444')
              AND EXISTS (SELECT 1 FROM restaurants WHERE id = 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa')
            ON CONFLICT (id) DO NOTHING;
        ");
    }
}

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
