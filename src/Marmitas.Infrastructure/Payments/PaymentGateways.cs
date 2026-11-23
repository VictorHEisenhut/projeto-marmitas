using System.Security.Cryptography;
using System.Text;
using Marmitas.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Marmitas.Infrastructure.Payments;

public abstract class BasePaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    protected BasePaymentGateway(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public abstract string ProviderName { get; }

    public Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.CardToken, "tok_declined", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentGatewayResult(
                false,
                ProviderName,
                $"{ProviderName.ToLowerInvariant()}_{Guid.NewGuid():N}"[..24],
                "Declined",
                null,
                null,
                "Transação recusada pela operadora do cartão."));
        }

        var externalId = $"{ProviderName.ToLowerInvariant()}_{Guid.NewGuid():N}"[..24];
        var isPix = string.Equals(request.Method, "PIX", StringComparison.OrdinalIgnoreCase);
        var pixCopyPaste = isPix
            ? $"00020126580014BR.GOV.BCB.PIX0136{request.OrderId}520400005303986540{request.Amount:F2}5802BR5915MARMITASMARKET6008SAOPAULO62070503***6304ABCD"
            : null;

        return Task.FromResult(new PaymentGatewayResult(
            true,
            ProviderName,
            externalId,
            "Approved",
            isPix ? $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(pixCopyPaste!)}" : null,
            pixCopyPaste));
    }

    public Task<PaymentGatewayResult> RefundAsync(string externalTransactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        var refundId = $"ref_{ProviderName.ToLowerInvariant()}_{Guid.NewGuid():N}"[..24];
        return Task.FromResult(new PaymentGatewayResult(
            true,
            ProviderName,
            refundId,
            "Refunded",
            null,
            null));
    }

    public bool ValidateWebhookSignature(string payload, string? signature)
    {
        var secret = _configuration["Payments:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return true;

        if (string.IsNullOrWhiteSpace(signature))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return string.Equals(computed, signature.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }
}

public sealed class MercadoPagoPaymentGateway : BasePaymentGateway
{
    public MercadoPagoPaymentGateway(IConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "MercadoPago";
}

public sealed class StripePaymentGateway : BasePaymentGateway
{
    public StripePaymentGateway(IConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "Stripe";
}

public sealed class PagarMePaymentGateway : BasePaymentGateway
{
    public PagarMePaymentGateway(IConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "PagarMe";
}

public sealed class AsaasPaymentGateway : BasePaymentGateway
{
    public AsaasPaymentGateway(IConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "Asaas";
}

public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly IConfiguration _configuration;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways, IConfiguration configuration)
    {
        _gateways = gateways;
        _configuration = configuration;
    }

    public IPaymentGateway GetGateway(string? providerName = null)
    {
        var target = string.IsNullOrWhiteSpace(providerName)
            ? _configuration["Payments:DefaultGateway"] ?? "MercadoPago"
            : providerName.Trim();

        return _gateways.FirstOrDefault(g =>
                   string.Equals(g.ProviderName, target, StringComparison.OrdinalIgnoreCase))
               ?? _gateways.First();
    }
}
