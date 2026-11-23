namespace Marmitas.Domain.Entities;

public class OrderStatusHistory : Entity
{
    public Guid OrderId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByRole { get; set; }
    public string? Notes { get; set; }
}
