using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;
using Marmitas.Infrastructure.Persistence;
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
            .Where(r => r.Status == RestaurantStatus.Approved)
            .ToListAsync();
    }

    public async Task<Restaurant?> GetByIdAsync(Guid id)
    {
        return await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Product>> GetProductsByRestaurantIdAsync(Guid restaurantId)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.RestaurantId == restaurantId && p.IsActive)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Product>> GetActiveProductsByIdsAsync(Guid restaurantId, IEnumerable<Guid> productIds)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.RestaurantId == restaurantId && p.IsActive && productIds.Contains(p.Id))
            .ToListAsync();
    }
}

