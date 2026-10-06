using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);

    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(Guid id);

    Task<Role?> GetRoleByNameAsync(string name);

    Task AddAsync(User user);

    Task SaveChangesAsync();
}

