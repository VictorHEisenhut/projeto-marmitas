using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly AppDbContext _context;

    public RestaurantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Restaurant>> GetAllApprovedAsync()
    {
        return await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.Status == RestaurantStatus.Approved && r.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<List<Restaurant>> GetAllAsync()
    {
        return await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Restaurant?> GetByIdAsync(Guid id)
    {
        return await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
    }

    public async Task AddAsync(Restaurant restaurant)
    {
        await _context.Restaurants.AddAsync(restaurant);
    }

    public async Task<bool> IsUserAuthorizedAsync(Guid restaurantId, Guid userId)
    {
        var restaurant = await _context.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == restaurantId && r.DeletedAt == null);

        if (restaurant == null) return false;
        if (restaurant.OwnerId == userId) return true;

        return await _context.RestaurantUsers
            .AnyAsync(ru => ru.RestaurantId == restaurantId && ru.UserId == userId && ru.IsActive);
    }

    public async Task<List<ProductCategory>> GetCategoriesAsync(Guid? restaurantId)
    {
        var query = _context.ProductCategories.AsNoTracking().AsQueryable();
        if (restaurantId.HasValue)
            query = query.Where(c => c.RestaurantId == null || c.RestaurantId == restaurantId.Value);

        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<ProductCategory?> GetCategoryByIdAsync(Guid categoryId)
    {
        return await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task AddCategoryAsync(ProductCategory category)
    {
        await _context.ProductCategories.AddAsync(category);
    }

    public void RemoveCategory(ProductCategory category)
    {
        _context.ProductCategories.Remove(category);
    }

    public async Task<List<Product>> GetAllActiveProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(Guid productId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);
    }

    public async Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId, bool includeInactive = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.RestaurantId == restaurantId && p.DeletedAt == null);

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Product>> GetActiveProductsByIdsAsync(Guid restaurantId, IEnumerable<Guid> productIds)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.RestaurantId == restaurantId && p.IsActive && p.DeletedAt == null && productIds.Contains(p.Id))
            .ToListAsync();
    }

    public async Task AddProductAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public Task<List<ProductOption>> GetProductOptionsAsync(Guid productId)
    {
        return _context.ProductOptions
            .AsNoTracking()
            .Where(o => o.ProductId == productId && o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    public Task<ProductOption?> GetProductOptionByIdAsync(Guid id)
    {
        return _context.ProductOptions.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddProductOptionAsync(ProductOption option)
    {
        await _context.ProductOptions.AddAsync(option);
    }

    public void RemoveProductOption(ProductOption option)
    {
        _context.ProductOptions.Remove(option);
    }

    public Task<List<RestaurantHour>> GetRestaurantHoursAsync(Guid restaurantId)
    {
        return _context.RestaurantHours
            .AsNoTracking()
            .Where(h => h.RestaurantId == restaurantId)
            .OrderBy(h => h.DayOfWeek)
            .ToListAsync();
    }

    public async Task SetRestaurantHoursAsync(Guid restaurantId, IEnumerable<RestaurantHour> hours)
    {
        var existing = await _context.RestaurantHours
            .Where(h => h.RestaurantId == restaurantId)
            .ToListAsync();
        _context.RestaurantHours.RemoveRange(existing);
        await _context.RestaurantHours.AddRangeAsync(hours);
    }

    public Task<List<DeliveryZone>> GetDeliveryZonesAsync(Guid restaurantId)
    {
        return _context.DeliveryZones
            .AsNoTracking()
            .Where(z => z.RestaurantId == restaurantId && z.IsActive)
            .OrderBy(z => z.Neighborhood)
            .ToListAsync();
    }

    public async Task AddDeliveryZoneAsync(DeliveryZone zone)
    {
        await _context.DeliveryZones.AddAsync(zone);
    }

    public Task<DeliveryZone?> GetDeliveryZoneByIdAsync(Guid id)
    {
        return _context.DeliveryZones.FirstOrDefaultAsync(z => z.Id == id);
    }

    public void RemoveDeliveryZone(DeliveryZone zone)
    {
        _context.DeliveryZones.Remove(zone);
    }

    public Task<List<RestaurantUser>> GetRestaurantUsersAsync(Guid restaurantId)
    {
        return _context.RestaurantUsers
            .AsNoTracking()
            .Where(ru => ru.RestaurantId == restaurantId && ru.IsActive)
            .ToListAsync();
    }

    public async Task AddRestaurantUserAsync(RestaurantUser restaurantUser)
    {
        await _context.RestaurantUsers.AddAsync(restaurantUser);
    }

    public void RemoveRestaurantUser(RestaurantUser restaurantUser)
    {
        _context.RestaurantUsers.Remove(restaurantUser);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
