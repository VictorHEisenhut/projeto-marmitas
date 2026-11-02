using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface ICustomerProfileRepository
{
    Task<List<Address>> GetAddressesByUserAsync(Guid userId);
    Task<Address?> GetAddressByIdAsync(Guid id, Guid userId);
    Task AddAddressAsync(Address address);
    void RemoveAddress(Address address);

    Task<List<Favorite>> GetFavoritesByUserAsync(Guid userId);
    Task<Favorite?> GetFavoriteByIdAsync(Guid id, Guid userId);
    Task AddFavoriteAsync(Favorite favorite);
    void RemoveFavorite(Favorite favorite);

    Task<List<Notification>> GetNotificationsByUserAsync(Guid userId);
    Task<Notification?> GetNotificationByIdAsync(Guid id);
    Task AddNotificationAsync(Notification notification);
    void RemoveNotification(Notification notification);

    Task<NotificationPreference?> GetPreferencesByUserAsync(Guid userId);
    Task AddPreferencesAsync(NotificationPreference preference);

    Task SaveChangesAsync();
}

