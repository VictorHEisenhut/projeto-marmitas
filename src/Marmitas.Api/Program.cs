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
