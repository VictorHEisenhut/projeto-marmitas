using System.Security.Cryptography;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Integrations;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Errors;
using Marmitas.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Marmitas.Infrastructure.Payments;

public sealed class RestaurantIntegrationService : IRestaurantIntegrationService
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IMercadoPagoApi _mercadoPago;
    private readonly IntegrationSecretProtector _protector;
    private readonly ILogger<RestaurantIntegrationService> _logger;

    public RestaurantIntegrationService(
        IRestaurantRepository restaurants,
        IMercadoPagoApi mercadoPago,
        IntegrationSecretProtector protector,
        ILogger<RestaurantIntegrationService> logger)
    {
        _restaurants = restaurants;
        _mercadoPago = mercadoPago;
        _protector = protector;
        _logger = logger;
    }

    public async Task<RestaurantIntegrationStatusDto> GetStatusAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        await EnsureRestaurantExistsAsync(restaurantId);
        var current = await _restaurants.GetIntegrationAsync(restaurantId, RestaurantIntegration.MercadoPagoProvider);
        return Map(current);
    }

    public async Task<MercadoPagoCheckoutOptionsDto> GetCheckoutOptionsAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        await EnsureRestaurantExistsAsync(restaurantId);
        var current = await _restaurants.GetIntegrationAsync(restaurantId, RestaurantIntegration.MercadoPagoProvider);
        var live = IsLive(current);

        return new MercadoPagoCheckoutOptionsDto
        {
            Provider = RestaurantIntegration.MercadoPagoProvider,
            Enabled = current?.Enabled == true,
            Live = live,
            PublicKey = live ? current!.PublicKey : null,
            Methods = live
                ? new List<string> { "PIX", "CreditCard" }
                : new List<string> { "PIX", "CreditCard", "DebitCard" }
        };
    }

    public async Task<RestaurantIntegrationStatusDto> SaveMercadoPagoAsync(
        Guid restaurantId,
        SaveMercadoPagoIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureRestaurantExistsAsync(restaurantId);

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (!IsMercadoPagoCredential(publicKey))
            throw DomainException.Validation("Informe a Public Key do Mercado Pago. Ela começa com TEST- ou APP_USR-.");

        var current = await _restaurants.GetIntegrationAsync(restaurantId, RestaurantIntegration.MercadoPagoProvider);
        var incomingToken = request.AccessToken?.Trim() ?? string.Empty;
        var keepCurrentToken = string.IsNullOrWhiteSpace(incomingToken) || incomingToken.Contains('•');

        string plainToken;
        if (keepCurrentToken)
        {
            if (current == null || string.IsNullOrWhiteSpace(current.AccessToken))
                throw DomainException.Validation("Informe o Access Token do Mercado Pago.");

            try
            {
                plainToken = _protector.Unprotect(current.AccessToken);
            }
            catch (CryptographicException)
            {
                throw DomainException.Validation("Não foi possível ler o token salvo. Informe o Access Token novamente.");
            }
        }
        else
        {
            plainToken = incomingToken;
        }

        if (!IsMercadoPagoCredential(plainToken))
            throw DomainException.Validation("O Access Token do Mercado Pago deve começar com TEST- ou APP_USR-.");

        if (CredentialFamily(plainToken) != CredentialFamily(publicKey))
            throw DomainException.Validation("O Access Token e a Public Key precisam ser do mesmo ambiente, teste ou produção.");

        var account = await _mercadoPago.GetAccountAsync(plainToken, cancellationToken);

        if (current == null)
        {
            current = new RestaurantIntegration
            {
                RestaurantId = restaurantId,
                Provider = RestaurantIntegration.MercadoPagoProvider
            };
            await _restaurants.AddIntegrationAsync(current);
        }

        current.AccessToken = _protector.Protect(plainToken);
        current.PublicKey = publicKey;
        current.Enabled = request.Enabled;
        current.AccountId = account.Id;
        current.AccountEmail = account.Email;
        await _restaurants.SaveChangesAsync();

        return Map(current, plainToken);
    }

    public async Task DisconnectMercadoPagoAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        await EnsureRestaurantExistsAsync(restaurantId);
        var current = await _restaurants.GetIntegrationAsync(restaurantId, RestaurantIntegration.MercadoPagoProvider);
        if (current == null)
            return;

        _restaurants.RemoveIntegration(current);
        await _restaurants.SaveChangesAsync();
    }

    public async Task<string?> GetAccessTokenAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var current = await _restaurants.GetIntegrationAsync(restaurantId, RestaurantIntegration.MercadoPagoProvider);
        if (!IsLive(current))
            return null;

        try
        {
            var token = _protector.Unprotect(current!.AccessToken);
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Falha ao ler o Access Token do restaurante {RestaurantId}", restaurantId);
            return null;
        }
    }

    private async Task EnsureRestaurantExistsAsync(Guid restaurantId)
    {
        var restaurant = await _restaurants.GetByIdAsync(restaurantId);
        if (restaurant == null)
            throw DomainException.NotFound("Restaurante não encontrado.");
    }

    private RestaurantIntegrationStatusDto Map(RestaurantIntegration? integration, string? knownToken = null)
    {
        if (integration == null || string.IsNullOrWhiteSpace(integration.AccessToken))
        {
            return new RestaurantIntegrationStatusDto
            {
                Provider = RestaurantIntegration.MercadoPagoProvider,
                Connected = false,
                Enabled = false
            };
        }

        string? token = knownToken;
        if (token == null)
        {
            try
            {
                token = _protector.Unprotect(integration.AccessToken);
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Token salvo do restaurante {RestaurantId} não pôde ser lido", integration.RestaurantId);
            }
        }

        return new RestaurantIntegrationStatusDto
        {
            Provider = integration.Provider,
            Enabled = integration.Enabled,
            Connected = true,
            PublicKey = integration.PublicKey,
            AccessTokenHint = string.IsNullOrWhiteSpace(token) ? "••••" : Mask(token),
            AccountEmail = integration.AccountEmail,
            AccountId = integration.AccountId,
            Environment = CredentialFamily(integration.PublicKey) == "TEST" ? "test" : "production",
            UpdatedAt = integration.UpdatedAt == default ? integration.CreatedAt : integration.UpdatedAt
        };
    }

    private static bool IsLive(RestaurantIntegration? integration) =>
        integration is { Enabled: true } &&
        IsMercadoPagoCredential(integration.PublicKey) &&
        !string.IsNullOrWhiteSpace(integration.AccessToken);

    private static bool IsMercadoPagoCredential(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        return trimmed.StartsWith("TEST-", StringComparison.Ordinal) ||
               trimmed.StartsWith("APP_USR-", StringComparison.Ordinal);
    }

    private static string CredentialFamily(string value) =>
        value.Trim().StartsWith("TEST-", StringComparison.Ordinal) ? "TEST" : "APP_USR";

    private static string Mask(string token)
    {
        var visible = token.Length <= 4 ? token : token[^4..];
        return $"••••{visible}";
    }
}
