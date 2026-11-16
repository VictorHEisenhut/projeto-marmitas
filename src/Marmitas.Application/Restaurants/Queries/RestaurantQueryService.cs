using System.Text.Json;
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
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;

    public RestaurantQueryService(IRestaurantRepository restaurantRepository, IUserRepository userRepository)
    {
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
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
            throw new Exception("O CNPJ do restaurante é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.AddressStreet) ||
            string.IsNullOrWhiteSpace(request.AddressNumber) ||
            string.IsNullOrWhiteSpace(request.AddressNeighborhood) ||
            string.IsNullOrWhiteSpace(request.AddressCity) ||
            string.IsNullOrWhiteSpace(request.AddressState) ||
            string.IsNullOrWhiteSpace(request.AddressZipCode))
        {
            throw new Exception("O endereço completo do restaurante (CEP, Rua, Número, Bairro, Cidade e UF) é obrigatório.");
        }

        var resolvedOwnerId = request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty
            ? request.OwnerId.Value
            : ownerId;

        var restaurant = new Restaurant(
            request.Name,
            request.Description ?? string.Empty,
            request.Document,
            resolvedOwnerId)
        {
            ImageUrl = request.ImageUrl,
            AddressStreet = request.AddressStreet.Trim(),
            AddressNumber = request.AddressNumber.Trim(),
            AddressComplement = request.AddressComplement?.Trim(),
            AddressNeighborhood = request.AddressNeighborhood.Trim(),
            AddressCity = request.AddressCity.Trim(),
            AddressState = request.AddressState.Trim().ToUpperInvariant(),
            AddressZipCode = request.AddressZipCode.Trim()
        };

        restaurant.Approve();

        await _restaurantRepository.AddAsync(restaurant);
        await EnsureOwnerRoleAsync(resolvedOwnerId);
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

        if (string.IsNullOrWhiteSpace(request.Document))
            throw new Exception("O CNPJ do restaurante é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.AddressStreet) ||
            string.IsNullOrWhiteSpace(request.AddressNumber) ||
            string.IsNullOrWhiteSpace(request.AddressNeighborhood) ||
            string.IsNullOrWhiteSpace(request.AddressCity) ||
            string.IsNullOrWhiteSpace(request.AddressState) ||
            string.IsNullOrWhiteSpace(request.AddressZipCode))
        {
            throw new Exception("O endereço completo do restaurante (CEP, Rua, Número, Bairro, Cidade e UF) é obrigatório.");
        }

        restaurant.Name = request.Name.Trim();
        restaurant.Description = (request.Description ?? string.Empty).Trim();
        restaurant.Document = request.Document.Trim();
        restaurant.ImageUrl = request.ImageUrl;
        restaurant.AddressStreet = request.AddressStreet.Trim();
        restaurant.AddressNumber = request.AddressNumber.Trim();
        restaurant.AddressComplement = request.AddressComplement?.Trim();
        restaurant.AddressNeighborhood = request.AddressNeighborhood.Trim();
        restaurant.AddressCity = request.AddressCity.Trim();
        restaurant.AddressState = request.AddressState.Trim().ToUpperInvariant();
        restaurant.AddressZipCode = request.AddressZipCode.Trim();

        if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
        {
            restaurant.OwnerId = request.OwnerId.Value;
            await EnsureOwnerRoleAsync(request.OwnerId.Value);
        }

        await _restaurantRepository.SaveChangesAsync();
        return MapRestaurant(restaurant);
    }

    private async Task EnsureOwnerRoleAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.GetRoleNames().Contains("restaurant_owner"))
            return;

        var ownerRole = await _userRepository.GetRoleByNameAsync("restaurant_owner");
        if (ownerRole != null)
        {
            user.AddRole(ownerRole);
        }
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
        return categories
            .GroupBy(c => c.Name.Trim().ToLower())
            .Select(g => g.OrderBy(c => c.RestaurantId.HasValue ? 1 : 0).First())
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
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

        if (request.RestaurantId.HasValue && request.RestaurantId.Value != Guid.Empty)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId.Value);
            if (restaurant == null)
                throw new Exception("Restaurante não encontrado.");
        }

        var category = new ProductCategory(
            request.RestaurantId == Guid.Empty ? null : request.RestaurantId,
            request.Name,
            request.DisplayOrder);

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

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tradicionais",
        "Fitness & Low Carb",
        "Executiva",
        "Vegetariana & Vegana",
        "Bebidas & Sobremesas"
    };

    public async Task<ProductDto> CreateProductAsync(Guid restaurantId, CreateProductRequest request)
    {
        ValidateProductRequest(request);

        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        var category = await _restaurantRepository.GetCategoryByIdAsync(request.CategoryId);
        if (category == null || (category.RestaurantId.HasValue && category.RestaurantId != restaurantId))
            throw new Exception("Categoria não encontrada para este restaurante.");

        if (!AllowedCategories.Contains(category.Name.Trim()))
            throw new Exception("Categoria inválida. Categorias permitidas: Tradicionais, Fitness & Low Carb, Executiva, Vegetariana & Vegana, Bebidas & Sobremesas.");

        var sanitizedAddons = SanitizeAddons(request.Addons);
        var cutleryPrice = request.AllowsCutlery && request.CutleryHasExtraCost ? request.CutleryPrice : 0m;

        var product = new Product(
            restaurantId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl)
        {
            AvailableInPlans = request.AvailableInPlans,
            AllowsCutlery = request.AllowsCutlery,
            CutleryHasExtraCost = request.AllowsCutlery && request.CutleryHasExtraCost,
            CutleryPrice = cutleryPrice,
            AddonsJson = JsonSerializer.Serialize(sanitizedAddons)
        };

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

        ValidateProductRequest(request);

        if (request.CategoryId != Guid.Empty)
        {
            var category = await _restaurantRepository.GetCategoryByIdAsync(request.CategoryId);
            if (category == null || !AllowedCategories.Contains(category.Name.Trim()))
                throw new Exception("Categoria inválida.");
            product.CategoryId = request.CategoryId;
        }

        var sanitizedAddons = SanitizeAddons(request.Addons);

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.AvailableInPlans = request.AvailableInPlans;
        product.AllowsCutlery = request.AllowsCutlery;
        product.CutleryHasExtraCost = request.AllowsCutlery && request.CutleryHasExtraCost;
        product.CutleryPrice = request.AllowsCutlery && request.CutleryHasExtraCost ? request.CutleryPrice : 0m;
        product.AddonsJson = JsonSerializer.Serialize(sanitizedAddons);

        await _restaurantRepository.SaveChangesAsync();
        return MapProduct(product);
    }

    private static void ValidateProductRequest(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome da marmita é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("A descrição da marmita é obrigatória.");

        if (request.CategoryId == Guid.Empty)
            throw new Exception("A categoria é obrigatória.");

        if (request.Price <= 0)
            throw new Exception("O valor avulso da marmita deve ser maior que zero.");

        if (request.AllowsCutlery && request.CutleryHasExtraCost && request.CutleryPrice <= 0)
            throw new Exception("Informe um valor maior que zero para o custo extra de talheres.");

        if (request.Addons != null)
        {
            foreach (var addon in request.Addons)
            {
                if (string.IsNullOrWhiteSpace(addon.Name))
                    throw new Exception("Todos os adicionais devem possuir um nome.");
                if (addon.ExtraPrice < 0)
                    throw new Exception($"O valor extra do adicional '{addon.Name}' não pode ser negativo.");
                if (addon.MaxQuantity <= 0)
                    throw new Exception($"O limite máximo do adicional '{addon.Name}' deve ser de pelo menos 1.");
            }
        }
    }

    private static List<ProductAddonDto> SanitizeAddons(List<ProductAddonDto>? addons)
    {
        if (addons == null) return new List<ProductAddonDto>();
        return addons.Select(a => new ProductAddonDto
        {
            Name = a.Name.Trim(),
            ExtraPrice = a.ExtraPrice,
            MaxQuantity = a.MaxQuantity
        }).ToList();
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
        OwnerId = r.OwnerId,
        AddressStreet = r.AddressStreet,
        AddressNumber = r.AddressNumber,
        AddressComplement = r.AddressComplement,
        AddressNeighborhood = r.AddressNeighborhood,
        AddressCity = r.AddressCity,
        AddressState = r.AddressState,
        AddressZipCode = r.AddressZipCode
    };

    private static ProductDto MapProduct(Product p)
    {
        List<ProductAddonDto> addons = new();
        if (!string.IsNullOrWhiteSpace(p.AddonsJson))
        {
            try
            {
                addons = JsonSerializer.Deserialize<List<ProductAddonDto>>(p.AddonsJson) ?? new();
            }
            catch
            {
                addons = new();
            }
        }

        return new ProductDto
        {
            Id = p.Id,
            RestaurantId = p.RestaurantId,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : string.Empty,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl,
            AvailableInPlans = p.AvailableInPlans,
            AllowsCutlery = p.AllowsCutlery,
            CutleryHasExtraCost = p.CutleryHasExtraCost,
            CutleryPrice = p.CutleryPrice,
            Addons = addons
        };
    }
}
