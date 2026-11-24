using System.Security.Claims;
using System.Text.Json;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/geolocalizacao")]
[ApiController]
public class GeolocationController : ControllerBase
{
    private static readonly Dictionary<string, (string Street, string Neighborhood, string City, string State, double Lat, double Lon)> KnownZipPrefixes = new()
    {
        ["89010"] = ("Rua XV de Novembro", "Centro", "Blumenau", "SC", -26.9194, -49.0661),
        ["89012"] = ("Rua Antônio da Veiga", "Victor Konder", "Blumenau", "SC", -26.9068, -49.0772),
        ["89020"] = ("Rua Amazonas", "Garcia", "Blumenau", "SC", -26.9355, -49.0638),
        ["89030"] = ("Rua Bahia", "Salto", "Blumenau", "SC", -26.9012, -49.1045),
        ["89035"] = ("Rua Almirante Barroso", "Vila Nova", "Blumenau", "SC", -26.9094, -49.0867),
        ["89036"] = ("Rua João Pessoa", "Velha", "Blumenau", "SC", -26.9242, -49.0932),
        ["89050"] = ("Rua República Argentina", "Ponta Aguda", "Blumenau", "SC", -26.9147, -49.0558),
        ["89052"] = ("Rua 2 de Setembro", "Itoupava Norte", "Blumenau", "SC", -26.8856, -49.0731)
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRestaurantRepository _restaurants;
    private readonly ICustomerProfileRepository _profiles;
    private readonly IDeliveryRepository _deliveries;
    private readonly IOrderRepository _orders;

    public GeolocationController(
        IHttpClientFactory httpClientFactory,
        IRestaurantRepository restaurants,
        ICustomerProfileRepository profiles,
        IDeliveryRepository deliveries,
        IOrderRepository orders)
    {
        _httpClientFactory = httpClientFactory;
        _restaurants = restaurants;
        _profiles = profiles;
        _deliveries = deliveries;
        _orders = orders;
    }

    [HttpGet("cep/{cep}")]
    [AllowAnonymous]
    public async Task<IActionResult> LookupZipCode(string cep, CancellationToken cancellationToken)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            throw new Exception("Informe um CEP válido com 8 dígitos.");

        var prefix5 = digits[..5];
        var hasKnownBlumenau = KnownZipPrefixes.TryGetValue(prefix5, out var knownMatch);

        string resolvedStreet = hasKnownBlumenau ? knownMatch.Street : string.Empty;
        string resolvedNeighborhood = hasKnownBlumenau ? knownMatch.Neighborhood : string.Empty;
        string resolvedCity = hasKnownBlumenau ? knownMatch.City : string.Empty;
        string resolvedState = hasKnownBlumenau ? knownMatch.State : string.Empty;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            using var response = await client.GetAsync($"https://viacep.com.br/ws/{digits}/json/", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                if (!json.TryGetProperty("erro", out _))
                {
                    var street = json.TryGetProperty("logradouro", out var l) ? l.GetString() : null;
                    var neighborhood = json.TryGetProperty("bairro", out var b) ? b.GetString() : null;
                    var city = json.TryGetProperty("localidade", out var c) ? c.GetString() : null;
                    var state = json.TryGetProperty("uf", out var u) ? u.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(street)) resolvedStreet = street.Trim();
                    if (!string.IsNullOrWhiteSpace(neighborhood)) resolvedNeighborhood = neighborhood.Trim();
                    if (!string.IsNullOrWhiteSpace(city)) resolvedCity = city.Trim();
                    if (!string.IsNullOrWhiteSpace(state)) resolvedState = state.Trim().ToUpperInvariant();
                }
            }
        }
        catch
        {
            // Usa fallback determinístico caso offline ou em ambiente sandboxed
        }

        double? resolvedLat = hasKnownBlumenau ? knownMatch.Lat : null;
        double? resolvedLon = hasKnownBlumenau ? knownMatch.Lon : null;

        if (!hasKnownBlumenau && !string.IsNullOrWhiteSpace(resolvedCity))
        {
            (resolvedLat, resolvedLon) = await TryGeocodeWithNominatimAsync(
                resolvedStreet,
                resolvedNeighborhood,
                resolvedCity,
                resolvedState,
                cancellationToken);
        }

        var point = GeoCalculator.ResolveAddressCoordinates(
            resolvedLat,
            resolvedLon,
            resolvedStreet,
            resolvedNeighborhood,
            resolvedCity,
            resolvedState,
            digits);

        return Ok(new
        {
            zipCode = $"{digits[..5]}-{digits[5..]}",
            street = resolvedStreet,
            neighborhood = resolvedNeighborhood,
            city = resolvedCity,
            state = resolvedState,
            latitude = point.Latitude,
            longitude = point.Longitude
        });
    }

    private async Task<(double? Lat, double? Lon)> TryGeocodeWithNominatimAsync(
        string street,
        string neighborhood,
        string city,
        string state,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryParts = new[] { street, neighborhood, city, state, "Brazil" }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var q = Uri.EscapeDataString(string.Join(", ", queryParts));
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2.5);
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=br&q={q}");
            req.Headers.UserAgent.ParseAdd("ProjetoMarmita/1.0");

            using var resp = await client.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode) return (null, null);

            var results = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
            {
                var first = results[0];
                if (first.TryGetProperty("lat", out var latProp) &&
                    first.TryGetProperty("lon", out var lonProp) &&
                    double.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
                {
                    return (lat, lon);
                }
            }
        }
        catch
        {
            // Fallback determinístico por cidade/UF se geocoding externo estiver indisponível
        }

        return (null, null);
    }

    [HttpGet("distancia")]
    [AllowAnonymous]
    public IActionResult CalculateDistance(
        [FromQuery] double originLat,
        [FromQuery] double originLon,
        [FromQuery] double destLat,
        [FromQuery] double destLon)
    {
        var distanceKm = GeoCalculator.CalculateDistanceKm(originLat, originLon, destLat, destLon);
        var fee = GeoCalculator.CalculateDeliveryFee(distanceKm);
        var estimatedMinutes = Math.Max(15, (int)Math.Round(15 + distanceKm * 3.5));

        return Ok(new
        {
            distanceKm,
            deliveryFee = fee,
            estimatedMinutes
        });
    }

    [HttpGet("estimativa-entrega")]
    [Authorize]
    public async Task<IActionResult> EstimateDeliveryForCart(
        [FromQuery] Guid restaurantId,
        [FromQuery] Guid? addressId = null)
    {
        var restaurant = await _restaurants.GetByIdAsync(restaurantId);
        if (restaurant == null)
            throw new Exception("Restaurante não encontrado.");

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        var addresses = userId != Guid.Empty
            ? await _profiles.GetAddressesByUserAsync(userId)
            : new List<Domain.Entities.Address>();

        var selectedAddress = addressId.HasValue
            ? addresses.FirstOrDefault(a => a.Id == addressId.Value)
            : addresses.FirstOrDefault(a => a.IsMain) ?? addresses.FirstOrDefault();

        var restaurantAddress = !string.IsNullOrWhiteSpace(restaurant.AddressStreet)
            ? $"{restaurant.AddressStreet}, {restaurant.AddressNumber} - {restaurant.AddressNeighborhood}, {restaurant.AddressCity}/{restaurant.AddressState}"
            : restaurant.Name;

        var customerAddress = selectedAddress != null
            ? $"{selectedAddress.Street}, {selectedAddress.Number} - {selectedAddress.Neighborhood}, {selectedAddress.City}/{selectedAddress.State}"
            : "Centro, Blumenau/SC";

        var restaurantPoint = GeoCalculator.ResolveAddressCoordinates(
            restaurant.Latitude,
            restaurant.Longitude,
            restaurant.AddressStreet,
            restaurant.AddressNeighborhood,
            restaurant.AddressCity,
            restaurant.AddressState,
            restaurant.AddressZipCode,
            restaurant.Name);

        var customerPoint = GeoCalculator.ResolveAddressCoordinates(
            selectedAddress?.Latitude,
            selectedAddress?.Longitude,
            selectedAddress?.Street,
            selectedAddress?.Neighborhood,
            selectedAddress?.City ?? "Blumenau",
            selectedAddress?.State ?? "SC",
            selectedAddress?.ZipCode,
            "Casa do cliente");

        // Considera entregas ativas em andamento do mesmo restaurante como possíveis paradas simultâneas na fila de rota
        var activeRestaurantOrders = (await _orders.GetByRestaurantIdAsync(restaurantId))
            .Where(o => o.Status is Domain.Enums.OrderStatus.ReadyForPickup
                or Domain.Enums.OrderStatus.DriverAssigned
                or Domain.Enums.OrderStatus.PickedUp
                or Domain.Enums.OrderStatus.InTransit)
            .OrderBy(o => o.CreatedAt)
            .Take(2)
            .ToList();

        var activeStops = activeRestaurantOrders
            .Select((o, idx) => GeoCalculator.ResolveAddressCoordinates(
                null,
                null,
                o.DeliveryAddress,
                $"Parada {idx + 1}"))
            .ToList();

        var eta = GeoCalculator.EstimateDeliveryEta(
            restaurantPoint,
            customerPoint,
            "Created",
            null,
            activeStops);

        return Ok(new
        {
            restaurantId,
            restaurantName = restaurant.Name,
            restaurantAddress,
            customerAddress,
            estimatedMinutes = eta.EstimatedMinutes,
            estimatedArrivalAt = eta.EstimatedArrivalAt,
            totalDistanceKm = eta.TotalDistanceKm,
            stopsAhead = eta.StopsAhead,
            preparationMinutes = eta.PreparationMinutes,
            travelMinutes = eta.TravelMinutes,
            stopsDelayMinutes = eta.StopsDelayMinutes,
            routeSummary = eta.RouteSummary
        });
    }
}
