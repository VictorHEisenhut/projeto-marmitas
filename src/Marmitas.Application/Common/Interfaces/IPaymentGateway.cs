namespace Marmitas.Application.Common.Interfaces;

public record PaymentChargeRequest(
    Guid OrderId,
    decimal Amount,
    string Method,
    string? Gateway = null,
    string? CardToken = null,
    string? CustomerEmail = null,
    string? AccessToken = null,
    string? PaymentMethodId = null,
    string? IssuerId = null,
    int Installments = 1,
    string? PayerIdentificationType = null,
    string? PayerIdentificationNumber = null,
    string? Description = null,
    string? IdempotencyKey = null);

public record PaymentGatewayResult(
    bool Approved,
    string GatewayName,
    string ExternalTransactionId,
    string Status,
    string? PixQrCode,
    string? PixCopyPaste,
    string? FailureReason = null,
    bool Pending = false);

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundAsync(string externalTransactionId, decimal amount, CancellationToken cancellationToken = default);
    bool ValidateWebhookSignature(string payload, string? signature);
}

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(string? providerName = null);
}
