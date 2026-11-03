using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;

namespace Marmitas.Application.Restaurants.Queries;

public interface IRestaurantQueryService
{
    Task<List<RestaurantDto>> GetAllPublicAsync();
    Task<RestaurantDto> GetByIdAsync(Guid id);
    Task<RestaurantDto> CreateAsync(Guid ownerId, CreateRestaurantRequest request);
    Task<RestaurantDto> UpdateAsync(Guid id, CreateRestaurantRequest request);
    Task<RestaurantDto> ChangeStatusAsync(Guid id, string status);
    Task DeleteAsync(Guid id);
    Task<List<CategoryDto>> GetCategoriesAsync(Guid? restaurantId);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, CreateCategoryRequest request);
    Task DeleteCategoryAsync(Guid id);
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<List<ProductDto>> GetRestaurantProductsAsync(Guid restaurantId);
    Task<ProductDto> CreateProductAsync(Guid restaurantId, CreateProductRequest request);
    Task<ProductDto> UpdateProductAsync(Guid id, CreateProductRequest request);
    Task<ProductDto> ChangeProductAvailabilityAsync(Guid id, bool isActive);
    Task DeleteProductAsync(Guid id);
}

public class RestaurantQueryService : IRestaurantQueryService
{
    private static readonly (string Name, int DisplayOrder)[] DefaultCategories =
    {
        ("Tradicionais", 1),
        ("Fitness & Low Carb", 2),
        ("Executiva", 3),
        ("Vegetariana & Vegana", 4),
        ("Bebidas & Sobremesas", 5)
    };

    private readonly IRestaurantRepository _restaurantRepository;

    public RestaurantQueryService(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<List<RestaurantDto>> GetAllPublicAsync()
    {
        var restaurants = await _restaurantRepository.GetAllApprovedAsync();
        return restaurants.Select(MapRestaurant).ToList();
    }

    public async Task<RestaurantDto> GetByIdAsync(Guid id)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        return MapRestaurant(restaurant);
    }

    public async Task<RestaurantDto> CreateAsync(Guid ownerId, CreateRestaurantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do restaurante é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Document))
            throw new Exception("O CNPJ/CPF do restaurante é obrigatório.");

        var restaurant = new Restaurant(
            request.Name,
            request.Description ?? string.Empty,
            request.Document,
            ownerId)
        {
            ImageUrl = request.ImageUrl
        };

        restaurant.Approve();

        await _restaurantRepository.AddAsync(restaurant);
        foreach (var item in DefaultCategories)
        {
            await _restaurantRepository.AddCategoryAsync(
                new ProductCategory(restaurant.Id, item.Name, item.DisplayOrder));
        }

        await _restaurantRepository.SaveChangesAsync();

        return MapRestaurant(restaurant);
    }

    public async Task<RestaurantDto> UpdateAsync(Guid id, CreateRestaurantRequest request)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do restaurante é obrigatório.");

        restaurant.Name = request.Name.Trim();
        restaurant.Description = (request.Description ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(request.Document))
            restaurant.Document = request.Document.Trim();
        restaurant.ImageUrl = request.ImageUrl;

        await _restaurantRepository.SaveChangesAsync();
        return MapRestaurant(restaurant);
    }

    public async Task<RestaurantDto> ChangeStatusAsync(Guid id, string status)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        if (!Enum.TryParse<RestaurantStatus>(status, true, out var parsedStatus))
            throw new Exception("Status inválido. Use Approved, Rejected ou PendingApproval.");

        restaurant.Status = parsedStatus;
        await _restaurantRepository.SaveChangesAsync();

        return MapRestaurant(restaurant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        restaurant.DeletedAt = DateTime.UtcNow;
        restaurant.Status = RestaurantStatus.Rejected;
        await _restaurantRepository.SaveChangesAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(Guid? restaurantId)
    {
        var categories = await _restaurantRepository.GetCategoriesAsync(restaurantId);

        if (restaurantId.HasValue && categories.Count == 0)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId.Value);
            if (restaurant != null)
            {
                foreach (var item in DefaultCategories)
                {
                    await _restaurantRepository.AddCategoryAsync(
                        new ProductCategory(restaurant.Id, item.Name, item.DisplayOrder));
                }

                await _restaurantRepository.SaveChangesAsync();
                categories = await _restaurantRepository.GetCategoriesAsync(restaurantId);
            }
        }
        else if (!restaurantId.HasValue)
        {
            var restaurants = await _restaurantRepository.GetAllApprovedAsync();
            var addedAny = false;

            foreach (var restaurant in restaurants)
            {
                if (categories.Any(c => c.RestaurantId == restaurant.Id))
                    continue;

                foreach (var item in DefaultCategories)
                {
                    await _restaurantRepository.AddCategoryAsync(
                        new ProductCategory(restaurant.Id, item.Name, item.DisplayOrder));
                }

                addedAny = true;
            }

            if (addedAny)
            {
                await _restaurantRepository.SaveChangesAsync();
                categories = await _restaurantRepository.GetCategoriesAsync(null);
            }
        }

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            RestaurantId = c.RestaurantId,
            Name = c.Name,
            DisplayOrder = c.DisplayOrder
        }).ToList();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome da categoria é obrigatório.");

        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        var category = new ProductCategory(request.RestaurantId, request.Name, request.DisplayOrder);

        await _restaurantRepository.AddCategoryAsync(category);
        await _restaurantRepository.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder
        };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, CreateCategoryRequest request)
    {
        var category = await _restaurantRepository.GetCategoryByIdAsync(id);
        if (category == null)
            throw new Exception("Categoria não encontrada.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome da categoria é obrigatório.");

        category.Name = request.Name.Trim();
        category.DisplayOrder = request.DisplayOrder;
        await _restaurantRepository.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder
        };
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _restaurantRepository.GetCategoryByIdAsync(id);
        if (category == null)
            throw new Exception("Categoria não encontrada.");

        _restaurantRepository.RemoveCategory(category);
        await _restaurantRepository.SaveChangesAsync();
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _restaurantRepository.GetAllActiveProductsAsync();
        return products.Select(MapProduct).ToList();
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await _restaurantRepository.GetProductByIdAsync(id);
        if (product == null)
            throw new Exception("Marmita/Produto não encontrado.");

        return MapProduct(product);
    }

    public async Task<List<ProductDto>> GetRestaurantProductsAsync(Guid restaurantId)
    {
        var products = await _restaurantRepository.GetProductsByRestaurantIdAsync(restaurantId);
        return products.Select(MapProduct).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(Guid restaurantId, CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do produto é obrigatório.");

        if (request.Price <= 0)
            throw new Exception("O preço do produto deve ser maior que zero.");

        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        var category = await _restaurantRepository.GetCategoryByIdAsync(request.CategoryId);
        if (category == null || category.RestaurantId != restaurantId)
            throw new Exception("Categoria não encontrada para este restaurante.");

        var product = new Product(
            restaurantId,
            request.CategoryId,
            request.Name,
            request.Description ?? string.Empty,
            request.Price,
            request.ImageUrl);

        await _restaurantRepository.AddProductAsync(product);
        await _restaurantRepository.SaveChangesAsync();

        product.Category = category;
        return MapProduct(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, CreateProductRequest request)
    {
        var product = await _restaurantRepository.GetProductByIdAsync(id);
        if (product == null)
            throw new Exception("Marmita/Produto não encontrado.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do produto é obrigatório.");

        if (request.Price <= 0)
            throw new Exception("O preço do produto deve ser maior que zero.");

        if (request.CategoryId != Guid.Empty)
            product.CategoryId = request.CategoryId;

        product.Name = request.Name.Trim();
        product.Description = (request.Description ?? string.Empty).Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;

        await _restaurantRepository.SaveChangesAsync();
        return MapProduct(product);
    }

    public async Task<ProductDto> ChangeProductAvailabilityAsync(Guid id, bool isActive)
    {
        var product = await _restaurantRepository.GetProductByIdAsync(id);
        if (product == null)
            throw new Exception("Marmita/Produto não encontrado.");

        if (isActive)
            product.Activate();
        else
            product.Deactivate();

        await _restaurantRepository.SaveChangesAsync();
        return MapProduct(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _restaurantRepository.GetProductByIdAsync(id);
        if (product == null)
            throw new Exception("Marmita/Produto não encontrado.");

        product.Deactivate();
        product.DeletedAt = DateTime.UtcNow;
        await _restaurantRepository.SaveChangesAsync();
    }

    private static RestaurantDto MapRestaurant(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Document = r.Document,
        ImageUrl = r.ImageUrl,
        Status = r.Status.ToString(),
        OwnerId = r.OwnerId
    };

    private static ProductDto MapProduct(Product p) => new()
    {
        Id = p.Id,
        RestaurantId = p.RestaurantId,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : string.Empty,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        IsActive = p.IsActive,
        ImageUrl = p.ImageUrl
    };
}
