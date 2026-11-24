using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllApprovedAsync();
    Task<List<Restaurant>> GetAllAsync();
    Task<Restaurant?> GetByIdAsync(Guid id);
    Task AddAsync(Restaurant restaurant);
    Task<bool> IsUserAuthorizedAsync(Guid restaurantId, Guid userId);

    Task<List<ProductCategory>> GetCategoriesAsync(Guid? restaurantId);
    Task<ProductCategory?> GetCategoryByIdAsync(Guid categoryId);
    Task AddCategoryAsync(ProductCategory category);
    void RemoveCategory(ProductCategory category);

    Task<List<Product>> GetAllActiveProductsAsync();
    Task<Product?> GetProductByIdAsync(Guid productId);
    Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId, bool includeInactive = false);
    Task<List<Product>> GetActiveProductsByIdsAsync(Guid restaurantId, IEnumerable<Guid> productIds);
    Task AddProductAsync(Product product);

    Task<List<ProductOption>> GetProductOptionsAsync(Guid productId);
    Task<ProductOption?> GetProductOptionByIdAsync(Guid id);
    Task AddProductOptionAsync(ProductOption option);
    void RemoveProductOption(ProductOption option);

    Task<List<RestaurantHour>> GetRestaurantHoursAsync(Guid restaurantId);
    Task SetRestaurantHoursAsync(Guid restaurantId, IEnumerable<RestaurantHour> hours);

    Task<List<DeliveryZone>> GetDeliveryZonesAsync(Guid restaurantId);
    Task AddDeliveryZoneAsync(DeliveryZone zone);
    Task<DeliveryZone?> GetDeliveryZoneByIdAsync(Guid id);
    void RemoveDeliveryZone(DeliveryZone zone);

    Task<List<RestaurantUser>> GetRestaurantUsersAsync(Guid restaurantId);
    Task AddRestaurantUserAsync(RestaurantUser restaurantUser);
    void RemoveRestaurantUser(RestaurantUser restaurantUser);

    Task<RestaurantIntegration?> GetIntegrationAsync(Guid restaurantId, string provider);
    Task AddIntegrationAsync(RestaurantIntegration integration);
    void RemoveIntegration(RestaurantIntegration integration);

    Task SaveChangesAsync();
}
