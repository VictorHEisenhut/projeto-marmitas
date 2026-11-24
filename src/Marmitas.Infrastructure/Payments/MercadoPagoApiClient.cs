using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Errors;
using Marmitas.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Marmitas.Infrastructure.Payments;

public sealed class MercadoPagoApiClient : IMercadoPagoApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoApiClient> _logger;

    public MercadoPagoApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MercadoPagoApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MercadoPagoAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            throw DomainException.Validation(
                "Access Token recusado pelo Mercado Pago. Confira se ele está ativo e pertence à conta da loja.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mercado Pago recusou a validação da conta com status {Status}", (int)response.StatusCode);
            throw DomainException.Validation(ExtractError(body, "Não foi possível validar o Access Token no Mercado Pago."));
        }

        var user = JsonSerializer.Deserialize<MpUser>(body, JsonOptions)
                   ?? throw DomainException.Validation("Resposta inválida do Mercado Pago ao validar a conta.");

        return new MercadoPagoAccount(ReadId(user.Id), user.Email, user.Nickname);
    }

    public async Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw DomainException.Validation("A loja não possui Access Token do Mercado Pago.");

        if (request.Amount <= 0)
        {
            return new PaymentGatewayResult(true, "MercadoPago", $"free_{request.OrderId:N}"[..24], "Approved", null, null);
        }

        var method = NormalizeMethod(request.Method);
        var isPix = method == "PIX";
        var isCard = method is "CREDITCARD" or "DEBITCARD";

        if (!isPix && !isCard)
            throw DomainException.Validation("Forma de pagamento não suportada pelo Mercado Pago. Use PIX ou cartão de crédito.");

        if (isCard && string.IsNullOrWhiteSpace(request.CardToken))
            throw DomainException.Validation("O cartão precisa ser tokenizado no Mercado Pago antes da cobrança.");

        if (isCard && string.IsNullOrWhiteSpace(request.PaymentMethodId))
            throw DomainException.Validation("Não foi possível identificar a bandeira do cartão.");

        var email = request.CustomerEmail?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw DomainException.Validation("Informe um e-mail válido para o pagador.");

        var identificationNumber = Digits(request.PayerIdentificationNumber);
        var payload = new MpCreatePayment
        {
            TransactionAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Description = TrimDescription(request.Description ?? $"Pedido {request.OrderId:N}"),
            ExternalReference = request.OrderId.ToString(),
            NotificationUrl = BlankToNull(_configuration["Payments:MercadoPagoNotificationUrl"]),
            Payer = new MpPayer
            {
                Email = email,
                Identification = identificationNumber == null
                    ? null
                    : new MpIdentification
                    {
                        Type = string.IsNullOrWhiteSpace(request.PayerIdentificationType) ? "CPF" : request.PayerIdentificationType.Trim(),
                        Number = identificationNumber
                    }
            }
        };

        if (isPix)
        {
            payload.PaymentMethodId = "pix";
        }
        else
        {
            payload.Token = request.CardToken!.Trim();
            payload.PaymentMethodId = request.PaymentMethodId!.Trim();
            payload.Installments = request.Installments < 1 ? 1 : request.Installments;
            payload.IssuerId = BlankToNull(request.IssuerId);
            payload.BinaryMode = true;
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"pay-{request.OrderId:N}-{method}"
            : request.IdempotencyKey.Trim();

        return await SendPaymentAsync(HttpMethod.Post, "v1/payments", request.AccessToken, payload, idempotencyKey, cancellationToken);
    }

    public Task<PaymentGatewayResult> GetPaymentAsync(string accessToken, string paymentId, CancellationToken cancellationToken = default)
    {
        return SendPaymentAsync(HttpMethod.Get, $"v1/payments/{paymentId}", accessToken, null, null, cancellationToken);
    }

    public async Task<PaymentGatewayResult> RefundAsync(string accessToken, string paymentId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payload = new MpRefund { Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero) };
        var result = await SendPaymentAsync(
            HttpMethod.Post,
            $"v1/payments/{paymentId}/refunds",
            accessToken,
            payload,
            $"refund-{paymentId}-{Guid.NewGuid():N}",
            cancellationToken);

        if (result.Status is "Approved" or "Refunded")
            return result with { Approved = true, Pending = false, FailureReason = null };

        return result with
        {
            Approved = false,
            Pending = false,
            FailureReason = result.FailureReason ?? "Estorno recusado pelo Mercado Pago."
        };
    }

    private async Task<PaymentGatewayResult> SendPaymentAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? payload,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);

        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (TryReadPayment(body, out var payment))
            return MapPayment(payment);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mercado Pago retornou {StatusCode} em {Path}", (int)response.StatusCode, path);
            throw DomainException.Validation(ExtractError(body, "O Mercado Pago recusou a operação de pagamento."));
        }

        throw DomainException.Validation("Resposta inválida do Mercado Pago.");
    }

    private static bool TryReadPayment(string body, out MpPayment payment)
    {
        payment = new MpPayment();
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<MpPayment>(body, JsonOptions);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Status))
                return false;
            if (parsed.Id.ValueKind is not (JsonValueKind.Number or JsonValueKind.String))
                return false;

            payment = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static PaymentGatewayResult MapPayment(MpPayment payment)
    {
        var status = (payment.Status ?? string.Empty).Trim().ToLowerInvariant();
        var approved = status is "approved";
        var pending = status is "pending" or "in_process" or "authorized";
        var mapped = status switch
        {
            "approved" => "Approved",
            "pending" or "in_process" or "authorized" => "Pending",
            "cancelled" or "canceled" => "Cancelled",
            "refunded" or "charged_back" => "Refunded",
            _ => "Declined"
        };

        var failure = approved || pending || mapped == "Refunded"
            ? null
            : DescribeFailure(payment.StatusDetail);

        return new PaymentGatewayResult(
            approved,
            "MercadoPago",
            ReadId(payment.Id),
            mapped,
            payment.PointOfInteraction?.TransactionData?.QrCodeBase64,
            payment.PointOfInteraction?.TransactionData?.QrCode,
            failure,
            pending);
    }

    private static string DescribeFailure(string? statusDetail)
    {
        return (statusDetail ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "cc_rejected_bad_filled_card_number" => "Número do cartão inválido.",
            "cc_rejected_bad_filled_date" => "Data de validade do cartão inválida.",
            "cc_rejected_bad_filled_security_code" => "Código de segurança do cartão inválido.",
            "cc_rejected_bad_filled_other" => "Confira os dados do cartão e tente novamente.",
            "cc_rejected_insufficient_amount" => "Cartão sem limite suficiente.",
            "cc_rejected_call_for_authorize" => "O banco pediu autorização. Entre em contato com a operadora do cartão.",
            "cc_rejected_card_disabled" => "Cartão desabilitado. Entre em contato com o banco.",
            "cc_rejected_duplicated_payment" => "Pagamento duplicado. Aguarde e tente novamente.",
            "cc_rejected_high_risk" => "Pagamento recusado pela análise de risco do Mercado Pago.",
            "cc_rejected_invalid_installments" => "Quantidade de parcelas inválida para este cartão.",
            "cc_rejected_max_attempts" => "Limite de tentativas excedido. Use outro cartão ou PIX.",
            "cc_rejected_other_reason" => "O cartão foi recusado. Tente outro cartão ou PIX.",
            "rejected_by_bank" => "O banco recusou a transação.",
            _ => "Pagamento recusado pelo Mercado Pago."
        };
    }

    private static string ExtractError(string body, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            var error = JsonSerializer.Deserialize<MpErrorBody>(body, JsonOptions);
            var cause = error?.Cause?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Description))?.Description;
            if (!string.IsNullOrWhiteSpace(cause))
                return cause!;
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message!;
        }
        catch (JsonException)
        {
            return fallback;
        }

        return fallback;
    }

    private static string ReadId(JsonElement id) => id.ValueKind switch
    {
        JsonValueKind.Number => id.GetRawText(),
        JsonValueKind.String => id.GetString() ?? string.Empty,
        _ => string.Empty
    };

    private static string NormalizeMethod(string? method)
    {
        return (method ?? string.Empty).Replace("_", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
    }

    private static string? Digits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string TrimDescription(string description)
    {
        var trimmed = description.Trim();
        return trimmed.Length <= 180 ? trimmed : trimmed[..180];
    }

    private sealed class MpUser
    {
        [JsonPropertyName("id")]
        public JsonElement Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }
    }

    private sealed class MpCreatePayment
    {
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("payment_method_id")]
        public string? PaymentMethodId { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("installments")]
        public int? Installments { get; set; }

        [JsonPropertyName("issuer_id")]
        public string? IssuerId { get; set; }

        [JsonPropertyName("binary_mode")]
        public bool? BinaryMode { get; set; }

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("notification_url")]
        public string? NotificationUrl { get; set; }

        [JsonPropertyName("payer")]
        public MpPayer Payer { get; set; } = new();
    }

    private sealed class MpPayer
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("identification")]
        public MpIdentification? Identification { get; set; }
    }

    private sealed class MpIdentification
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "CPF";

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;
    }

    private sealed class MpRefund
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    private sealed class MpPayment
    {
        [JsonPropertyName("id")]
        public JsonElement Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; set; }

        [JsonPropertyName("point_of_interaction")]
        public MpPoint? PointOfInteraction { get; set; }
    }

    private sealed class MpPoint
    {
        [JsonPropertyName("transaction_data")]
        public MpTransactionData? TransactionData { get; set; }
    }

    private sealed class MpTransactionData
    {
        [JsonPropertyName("qr_code")]
        public string? QrCode { get; set; }

        [JsonPropertyName("qr_code_base64")]
        public string? QrCodeBase64 { get; set; }
    }

    private sealed class MpErrorBody
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("cause")]
        public List<MpCause>? Cause { get; set; }
    }

    private sealed class MpCause
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
