using Marmitas.Application.Extensions;
using Marmitas.Api.Extensions;
using Marmitas.Api.Hubs;
using Marmitas.Api.Middleware;
using Marmitas.Api.Workers;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Deliveries;
using Marmitas.Application.Financial;
using Marmitas.Application.Orders;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Application.Users;
using Marmitas.Infrastructure.Ai;
using Marmitas.Infrastructure.Cache;
using Marmitas.Infrastructure.Identity;
using Marmitas.Infrastructure.Messaging;
using Marmitas.Infrastructure.Payments;
using Marmitas.Infrastructure.Persistence;
using Marmitas.Infrastructure.Persistence.Repositories;
using Marmitas.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IPaymentGateway, MercadoPagoPaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, PagarMePaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, AsaasPaymentGateway>();
builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

builder.Services.AddApplicationServices();

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddHostedService<PlatformBackgroundWorker>();
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient<IAiAssistantService, GeminiAiAssistantService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services
    .AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsProduction() && allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    })
    .AddPlatformRateLimiting(builder.Configuration)
    .AddApiControllers()
    .AddSwaggerConfig()
    .AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapHealthChecks("/health");
app.MapHub<OrderTrackingHub>("/hubs/tracking");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
