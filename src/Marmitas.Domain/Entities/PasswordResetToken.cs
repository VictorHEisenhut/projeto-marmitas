namespace Marmitas.Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid => UsedAt == null && Attempts < 3 && ExpiresAt > DateTime.UtcNow;
}
