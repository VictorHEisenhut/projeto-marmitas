using System.Net;
using System.Text;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marmitas.UnitTests.Payments;

public class MercadoPagoIntegrationTests
{
    [Fact]
    public void Protetor_Roundtrip_PreservaOAccessToken()
    {
        var protector = new IntegrationSecretProtector(new MemoryConfig(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "chave-de-teste-para-integracao"
        }));

        const string token = "TEST-1234567890-abcdef";
        var stored = protector.Protect(token);

        Assert.StartsWith("enc:", stored);
        Assert.DoesNotContain(token, stored);
        Assert.Equal(token, protector.Unprotect(stored));
    }

    [Fact]
    public async Task Gateway_SemTokenDaLoja_MantemPagamentoSimulado()
    {
        var gateway = new MercadoPagoPaymentGateway(
            new MemoryConfig(new Dictionary<string, string?>()),
            new UnusedMercadoPagoApi());

        var result = await gateway.ChargeAsync(new PaymentChargeRequest(Guid.NewGuid(), 32m, "PIX"));

        Assert.True(result.Approved);
        Assert.False(result.Pending);
        Assert.False(string.IsNullOrWhiteSpace(result.PixCopyPaste));
    }

    [Fact]
    public async Task Cliente_MapeiaPixPendenteECartaoAprovado()
    {
        var handler = new StubHandler();
        var client = new MercadoPagoApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadopago.com/") },
            new MemoryConfig(new Dictionary<string, string?>()),
            NullLogger<MercadoPagoApiClient>.Instance);

        handler.Responder = request =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("TEST-token", request.Headers.Authorization?.Parameter);
            var json = """
                {
                  "id": 987654,
                  "status": "pending",
                  "status_detail": "pending_waiting_transfer",
                  "point_of_interaction": {
                    "transaction_data": {
                      "qr_code": "00020126580014BR.GOV.BCB.PIX",
                      "qr_code_base64": "aGVsbG8="
                    }
                  }
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        };

        var pix = await client.ChargeAsync(new PaymentChargeRequest(
            Guid.NewGuid(),
            41.9m,
            "PIX",
            AccessToken: "TEST-token",
            CustomerEmail: "cliente@email.com"));

        Assert.True(pix.Pending);
        Assert.False(pix.Approved);
        Assert.Equal("Pending", pix.Status);
        Assert.Equal("987654", pix.ExternalTransactionId);
        Assert.Equal("00020126580014BR.GOV.BCB.PIX", pix.PixCopyPaste);
        Assert.Equal("aGVsbG8=", pix.PixQrCode);

        handler.Responder = _ =>
        {
            var json = """
                { "id": 555, "status": "approved", "status_detail": "accredited" }
                """;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        };

        var card = await client.ChargeAsync(new PaymentChargeRequest(
            Guid.NewGuid(),
            18m,
            "CreditCard",
            AccessToken: "TEST-token",
            CardToken: "card-token",
            CustomerEmail: "cliente@email.com",
            PaymentMethodId: "visa"));

        Assert.True(card.Approved);
        Assert.False(card.Pending);
        Assert.Equal("Approved", card.Status);
        Assert.Equal("555", card.ExternalTransactionId);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Responder(request));
    }

    private sealed class UnusedMercadoPagoApi : IMercadoPagoApi
    {
        public Task<MercadoPagoAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A validação da conta não deveria rodar no pagamento simulado.");

        public Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A API do Mercado Pago não deveria ser chamada sem token da loja.");

        public Task<PaymentGatewayResult> GetPaymentAsync(string accessToken, string paymentId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException();

        public Task<PaymentGatewayResult> RefundAsync(string accessToken, string paymentId, decimal amount, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException();
    }

    private sealed class MemoryConfig : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public MemoryConfig(Dictionary<string, string?> values) => _values = values;

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => null!;

        public IConfigurationSection GetSection(string key) => null!;
    }
}
