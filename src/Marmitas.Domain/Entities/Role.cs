namespace Marmitas.Domain.Entities;

public class Role : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<RolePermission> RolePermissions { get; set; } = new();

    private Role() { }

    public Role(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

