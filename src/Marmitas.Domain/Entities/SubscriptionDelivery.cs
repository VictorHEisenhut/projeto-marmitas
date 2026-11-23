namespace Marmitas.Domain.Entities;

public class SubscriptionDelivery : Entity
{
    public Guid SubscriptionId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? AddressId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string DeliveryWindow { get; set; } = "11:30 - 13:30";
    public int MealsCount { get; set; } = 1;
    public string Status { get; set; } = "Scheduled";
}
