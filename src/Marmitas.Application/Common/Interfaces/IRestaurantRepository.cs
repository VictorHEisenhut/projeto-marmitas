using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllApprovedAsync();
    Task<Restaurant?> GetByIdAsync(Guid id);
    Task AddAsync(Restaurant restaurant);
    Task<List<ProductCategory>> GetCategoriesAsync(Guid? restaurantId);
    Task<ProductCategory?> GetCategoryByIdAsync(Guid categoryId);
    Task AddCategoryAsync(ProductCategory category);
    void RemoveCategory(ProductCategory category);
    Task<List<Product>> GetAllActiveProductsAsync();
    Task<Product?> GetProductByIdAsync(Guid productId);
    Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId);
    Task<List<Product>> GetActiveProductsByIdsAsync(Guid restaurantId, IEnumerable<Guid> productIds);
    Task AddProductAsync(Product product);
    Task SaveChangesAsync();
}
