namespace Marmitas.Application.Common.Interfaces;

public record MercadoPagoAccount(string Id, string? Email, string? Nickname);

public interface IMercadoPagoApi
{
    Task<MercadoPagoAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> GetPaymentAsync(string accessToken, string paymentId, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundAsync(string accessToken, string paymentId, decimal amount, CancellationToken cancellationToken = default);
}
