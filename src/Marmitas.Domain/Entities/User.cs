using Marmitas.Domain.Enums;

namespace Marmitas.Domain.Entities;

public class User : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();

    private User() { }

    public User(string name, string email, string? phone, string passwordHash)
    {
        Name = name.Trim();
        Email = email.Trim().ToLower();
        Phone = phone;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
    }

    public void AddRole(Role role)
    {
        UserRoles.Add(new UserRole(Id, role.Id) { Role = role });
    }

    public List<string> GetRoleNames()
    {
        var names = new List<string>();
        foreach (var userRole in UserRoles)
        {
            if (userRole.Role != null)
                names.Add(userRole.Role.Name);
        }
        return names;
    }

    public List<string> GetPermissionNames()
    {
        var names = new List<string>();
        foreach (var userRole in UserRoles)
        {
            if (userRole.Role == null) continue;

            foreach (var rolePermission in userRole.Role.RolePermissions)
            {
                if (rolePermission.Permission != null && !names.Contains(rolePermission.Permission.Name))
                    names.Add(rolePermission.Permission.Name);
            }
        }
        return names;
    }
}

