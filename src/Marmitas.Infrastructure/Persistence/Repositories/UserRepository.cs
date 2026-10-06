using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _context.Users.AnyAsync(u => u.Email == email);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<Role?> GetRoleByNameAsync(string name)
    {
        return _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<User> UsersWithRoles()
    {
        return _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role!)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission);
    }
}

