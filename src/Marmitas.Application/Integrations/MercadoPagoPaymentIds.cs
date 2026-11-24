namespace Marmitas.Application.Integrations;

public static class MercadoPagoPaymentIds
{
    public static bool IsLive(string? externalId) =>
        !string.IsNullOrWhiteSpace(externalId) && externalId.All(char.IsDigit);
}
