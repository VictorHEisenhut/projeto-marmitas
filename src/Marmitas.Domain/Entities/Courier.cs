namespace Marmitas.Domain.Entities;

public class Courier : Entity
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved";
    public bool IsAvailable { get; set; } = true;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Endereço exclusivo de cobrança/faturamento do entregador (não utilizado para entrega de pedidos)
    public string BillingStreet { get; set; } = string.Empty;
    public string BillingNumber { get; set; } = string.Empty;
    public string? BillingComplement { get; set; }
    public string BillingNeighborhood { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingState { get; set; } = string.Empty;
    public string BillingZipCode { get; set; } = string.Empty;
}
