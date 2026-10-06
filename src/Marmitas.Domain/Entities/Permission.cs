namespace Marmitas.Domain.Entities;

public class Permission : Entity
{

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private Permission() { }

    public Permission(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

