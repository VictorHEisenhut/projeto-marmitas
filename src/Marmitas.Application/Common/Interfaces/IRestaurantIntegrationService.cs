using Marmitas.Application.Integrations;

namespace Marmitas.Application.Common.Interfaces;

public interface IRestaurantIntegrationService
{
    Task<RestaurantIntegrationStatusDto> GetStatusAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<MercadoPagoCheckoutOptionsDto> GetCheckoutOptionsAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<RestaurantIntegrationStatusDto> SaveMercadoPagoAsync(Guid restaurantId, SaveMercadoPagoIntegrationRequest request, CancellationToken cancellationToken = default);
    Task DisconnectMercadoPagoAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(Guid restaurantId, CancellationToken cancellationToken = default);
}
