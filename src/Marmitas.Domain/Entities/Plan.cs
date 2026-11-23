namespace Marmitas.Domain.Entities;

public class Plan : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealsPerWeek { get; set; }
    public int MealsPerMonth { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string AllowedDays { get; set; } = "Segunda a Sexta";
    public string DeliveryWindow { get; set; } = "11:00 às 14:30";
    public decimal DeliveryFee { get; set; }
    public string CancellationPolicy { get; set; } = "Cancelamento livre até o fim do ciclo vigente.";
    public bool IsActive { get; set; } = true;
}
