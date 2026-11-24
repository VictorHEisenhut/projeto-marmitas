namespace Marmitas.Application.Integrations;

public class SaveMercadoPagoIntegrationRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class RestaurantIntegrationStatusDto
{
    public string Provider { get; set; } = "MercadoPago";
    public bool Enabled { get; set; }
    public bool Connected { get; set; }
    public string? PublicKey { get; set; }
    public string? AccessTokenHint { get; set; }
    public string? AccountEmail { get; set; }
    public string? AccountId { get; set; }
    public string? Environment { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MercadoPagoCheckoutOptionsDto
{
    public string Provider { get; set; } = "MercadoPago";
    public bool Enabled { get; set; }
    public bool Live { get; set; }
    public string? PublicKey { get; set; }
    public List<string> Methods { get; set; } = new();
}
