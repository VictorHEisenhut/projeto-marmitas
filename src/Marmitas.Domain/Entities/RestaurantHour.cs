namespace Marmitas.Domain.Entities;

public class RestaurantHour : Entity
{
    public Guid RestaurantId { get; set; }
    public int DayOfWeek { get; set; }
    public string OpenTime { get; set; } = "10:30";
    public string CloseTime { get; set; } = "15:30";
    public bool IsOpen { get; set; } = true;
}
