namespace Marmitas.Domain.Entities;

public class CouponUsage : Entity
{
    public Guid CouponId { get; set; }
    public Guid UserId { get; set; }
    public DateTime UsedAt { get; set; }
}

