using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllApprovedAsync();
    Task<Restaurant?> GetByIdAsync(Guid id);
    Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId);
    Task<List<Product>> GetActiveProductsByIdsAsync(Guid restaurantId, IEnumerable<Guid> productIds);
}

