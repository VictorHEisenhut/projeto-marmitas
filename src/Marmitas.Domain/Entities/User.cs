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

    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();

    private User() { }

    public User(string name, string email, string? phone, string passwordHash)
    {
        Name = name.Trim();
        Email = email.Trim().ToLower();
        Phone = phone;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        TermsAcceptedAt = DateTime.UtcNow;
    }

    public void AddRole(Role role)
    {
        UserRoles.Add(new UserRole(Id, role.Id) { Role = role });
    }

    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockoutEndAt = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutEndAt = null;
    }

    public bool IsLockedOut()
    {
        return LockoutEndAt.HasValue && LockoutEndAt.Value > DateTime.UtcNow;
    }

    public void Anonymize()
    {
        Name = "Titular Anonimizado";
        Email = $"deleted_{Id:N}@anon.marmitas.local";
        Phone = null;
        PasswordHash = string.Empty;
        MfaEnabled = false;
        MfaSecret = null;
        DeletedAt = DateTime.UtcNow;
        Status = UserStatus.Blocked;
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
