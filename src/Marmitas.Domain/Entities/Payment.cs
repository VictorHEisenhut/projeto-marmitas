namespace Marmitas.Domain.Entities;

public class Payment : Entity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "PIX";
    public string Status { get; set; } = "Pending";
}

