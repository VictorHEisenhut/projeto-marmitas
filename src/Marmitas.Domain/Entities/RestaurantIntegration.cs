namespace Marmitas.Domain.Entities;

public class RestaurantIntegration : Entity
{
    public const string MercadoPagoProvider = "MercadoPago";

    public Guid RestaurantId { get; set; }
    public string Provider { get; set; } = MercadoPagoProvider;
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? AccountId { get; set; }
    public string? AccountEmail { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
}
