namespace Marmitas.Domain.Entities;

public class NotificationPreference : Entity
{
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
}

