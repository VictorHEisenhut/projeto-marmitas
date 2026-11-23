namespace Marmitas.Domain.Entities;

public class UserConsent : Entity
{
    public Guid UserId { get; set; }
    public string Purpose { get; set; } = "TermsAndPrivacy";
    public string TermsVersion { get; set; } = "v1.0";
    public bool IsGranted { get; set; } = true;
    public string? IpAddress { get; set; }
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
}
