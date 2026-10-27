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
    Task SaveChangesAsync();
}
