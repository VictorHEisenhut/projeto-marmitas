using Marmitas.Application.Deliveries;
using Marmitas.Application.Financial;
using Marmitas.Application.Orders;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Marmitas.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRegisterUserService, RegisterUserService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRestaurantQueryService, RestaurantQueryService>();
        services.AddScoped<ICreateOrderService, CreateOrderService>();
        services.AddScoped<IFinancialLedgerService, FinancialLedgerService>();
        services.AddScoped<IDeliveryDistributionService, DeliveryDistributionService>();
        return services;
    }
}
