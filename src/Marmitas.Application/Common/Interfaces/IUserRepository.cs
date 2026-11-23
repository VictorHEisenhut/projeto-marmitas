using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllAsync();
    Task<Role?> GetRoleByNameAsync(string name);
    Task<Role?> GetRoleByIdAsync(Guid id);
    Task<List<Role>> GetAllRolesAsync();
    Task AddRoleAsync(Role role);
    Task<List<Permission>> GetAllPermissionsAsync();
    Task AddAsync(User user);

    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeUserRefreshTokensAsync(Guid userId);

    Task AddPasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(string email);

    Task AddUserConsentAsync(UserConsent consent);
    Task<List<UserConsent>> GetUserConsentsAsync(Guid userId);

    Task SaveChangesAsync();
}
