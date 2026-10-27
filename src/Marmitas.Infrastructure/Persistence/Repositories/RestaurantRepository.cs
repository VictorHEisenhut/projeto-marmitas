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

    public async Task<Restaurant?> GetByIdAsync(Guid id)
    {
        return await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
    }

    public async Task AddAsync(Restaurant restaurant)
    {
        await _context.Restaurants.AddAsync(restaurant);
    }

    public async Task<List<ProductCategory>> GetCategoriesAsync(Guid? restaurantId)
    {
        var query = _context.ProductCategories.AsNoTracking().AsQueryable();
        if (restaurantId.HasValue)
            query = query.Where(c => c.RestaurantId == restaurantId.Value);

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

    public async Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.RestaurantId == restaurantId && p.IsActive && p.DeletedAt == null)
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

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
