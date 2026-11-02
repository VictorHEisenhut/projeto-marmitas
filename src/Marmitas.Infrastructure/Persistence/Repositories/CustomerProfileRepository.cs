using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly AppDbContext _context;

    public CustomerProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Address>> GetAddressesByUserAsync(Guid userId)
    {
        return _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsMain)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public Task<Address?> GetAddressByIdAsync(Guid id, Guid userId)
    {
        return _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public async Task AddAddressAsync(Address address)
    {
        await _context.Addresses.AddAsync(address);
    }

    public void RemoveAddress(Address address)
    {
        _context.Addresses.Remove(address);
    }

    public Task<List<Favorite>> GetFavoritesByUserAsync(Guid userId)
    {
        return _context.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public Task<Favorite?> GetFavoriteByIdAsync(Guid id, Guid userId)
    {
        return _context.Favorites.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
    }

    public async Task AddFavoriteAsync(Favorite favorite)
    {
        await _context.Favorites.AddAsync(favorite);
    }

    public void RemoveFavorite(Favorite favorite)
    {
        _context.Favorites.Remove(favorite);
    }

    public Task<List<Notification>> GetNotificationsByUserAsync(Guid userId)
    {
        return _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public Task<Notification?> GetNotificationByIdAsync(Guid id)
    {
        return _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task AddNotificationAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public void RemoveNotification(Notification notification)
    {
        _context.Notifications.Remove(notification);
    }

    public Task<NotificationPreference?> GetPreferencesByUserAsync(Guid userId)
    {
        return _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task AddPreferencesAsync(NotificationPreference preference)
    {
        await _context.NotificationPreferences.AddAsync(preference);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}

