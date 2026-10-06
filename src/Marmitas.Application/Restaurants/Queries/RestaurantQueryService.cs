using Marmitas.Application.Common.Interfaces;

namespace Marmitas.Application.Restaurants.Queries;

public interface IRestaurantQueryService
{
    Task<List<RestaurantDto>> GetAllPublicAsync();
    Task<List<ProductDto>> GetRestaurantProductsAsync(Guid restaurantId);
}

public class RestaurantQueryService : IRestaurantQueryService
{
    private readonly IRestaurantRepository _restaurantRepository;

    public RestaurantQueryService(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<List<RestaurantDto>> GetAllPublicAsync()
    {
        var restaurants = await _restaurantRepository.GetAllApprovedAsync();
        return restaurants.Select(r => new RestaurantDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            ImageUrl = r.ImageUrl
        }).ToList();
    }

    public async Task<List<ProductDto>> GetRestaurantProductsAsync(Guid restaurantId)
    {
        var products = await _restaurantRepository.GetProductsByRestaurantIdAsync(restaurantId);
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : string.Empty,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl
        }).ToList();
    }
}
