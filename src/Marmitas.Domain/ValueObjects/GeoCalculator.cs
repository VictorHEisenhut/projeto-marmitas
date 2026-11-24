using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Marmitas.Domain.ValueObjects;

public sealed record GeoPoint(double Latitude, double Longitude, string Label = "");

public sealed record DeliveryEtaEstimate(
    int EstimatedMinutes,
    DateTime EstimatedArrivalAt,
    double TotalDistanceKm,
    int StopsAhead,
    int PreparationMinutes,
    int TravelMinutes,
    int StopsDelayMinutes,
    string RouteSummary);

public static class GeoCalculator
{
    private const double EarthRadiusKm = 6371.0;
    private const double UrbanRoutingFactor = 1.30;
    private const double AverageUrbanSpeedKmPerHour = 24.0;
    private const int MinutesPerIntermediateStop = 5;

    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusKm * c, 2);
    }

    public static decimal CalculateDeliveryFee(double distanceKm, decimal baseFee = 5.90m, decimal perKmFee = 1.20m)
    {
        if (distanceKm <= 2.0) return baseFee;
        var extraKm = (decimal)(distanceKm - 2.0);
        return Math.Round(baseFee + extraKm * perKmFee, 2);
    }

    public static DeliveryEtaEstimate EstimateDeliveryEta(
        GeoPoint restaurantPoint,
        GeoPoint customerPoint,
        string orderStatus,
        GeoPoint? driverPoint = null,
        IReadOnlyList<GeoPoint>? intermediateStops = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (string.Equals(orderStatus, "Delivered", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(orderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(orderStatus, "Refunded", StringComparison.OrdinalIgnoreCase))
        {
            return new DeliveryEtaEstimate(
                0,
                now,
                0,
                0,
                0,
                0,
                0,
                "Entrega finalizada");
        }

        var stops = intermediateStops ?? Array.Empty<GeoPoint>();
        var waypoints = new List<GeoPoint>();

        var isPickedUpOrInTransit =
            string.Equals(orderStatus, "PickedUp", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(orderStatus, "InTransit", StringComparison.OrdinalIgnoreCase);

        var isDriverAssigned = string.Equals(orderStatus, "DriverAssigned", StringComparison.OrdinalIgnoreCase);

        if (isPickedUpOrInTransit)
        {
            waypoints.Add(driverPoint ?? restaurantPoint);
        }
        else if (isDriverAssigned && driverPoint != null)
        {
            waypoints.Add(driverPoint);
            waypoints.Add(restaurantPoint);
        }
        else
        {
            waypoints.Add(restaurantPoint);
        }

        foreach (var stop in stops)
        {
            waypoints.Add(stop);
        }

        waypoints.Add(customerPoint);

        double rawDistanceKm = 0;
        for (var i = 0; i < waypoints.Count - 1; i++)
        {
            rawDistanceKm += CalculateDistanceKm(
                waypoints[i].Latitude,
                waypoints[i].Longitude,
                waypoints[i + 1].Latitude,
                waypoints[i + 1].Longitude);
        }

        var urbanDistanceKm = Math.Round(rawDistanceKm * UrbanRoutingFactor, 2);

        var prepMinutes = orderStatus switch
        {
            "Created" or "PaymentPending" or "Paid" or "Confirmed" => 18,
            "Preparing" => 12,
            "ReadyForPickup" => 6,
            "DriverAssigned" => 4,
            "PickedUp" or "InTransit" => 0,
            _ => 10
        };

        var travelMinutes = (int)Math.Ceiling((urbanDistanceKm / AverageUrbanSpeedKmPerHour) * 60.0);
        if (travelMinutes < 3 && urbanDistanceKm > 0)
            travelMinutes = 3;

        var stopsDelayMinutes = stops.Count * MinutesPerIntermediateStop;
        var finalBufferMinutes = 2;
        var totalMinutes = Math.Max(1, prepMinutes + travelMinutes + stopsDelayMinutes + finalBufferMinutes);

        var routeParts = new List<string>();
        if (!isPickedUpOrInTransit)
            routeParts.Add("Restaurante");
        else
            routeParts.Add("Entregador em rota");

        if (stops.Count == 1)
            routeParts.Add("1 parada anterior");
        else if (stops.Count > 1)
            routeParts.Add($"{stops.Count} paradas anteriores");

        routeParts.Add("Sua casa");

        return new DeliveryEtaEstimate(
            totalMinutes,
            now.AddMinutes(totalMinutes),
            urbanDistanceKm,
            stops.Count,
            prepMinutes,
            travelMinutes,
            stopsDelayMinutes,
            string.Join(" → ", routeParts));
    }

    private static readonly Dictionary<string, (double Lat, double Lon)> StateCentroids = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sc"] = (-27.2423, -50.2189),
        ["sp"] = (-23.5505, -46.6333),
        ["pr"] = (-25.4284, -49.2733),
        ["rs"] = (-30.0346, -51.2177),
        ["rj"] = (-22.9068, -43.1729),
        ["mg"] = (-19.9167, -43.9345),
        ["es"] = (-20.3155, -40.3128),
        ["df"] = (-15.7975, -47.8919),
        ["go"] = (-16.6869, -49.2648),
        ["mt"] = (-15.6014, -56.0979),
        ["ms"] = (-20.4697, -54.6201),
        ["ba"] = (-12.9714, -38.5014),
        ["pe"] = (-8.0476, -34.8770),
        ["ce"] = (-3.7319, -38.5267),
        ["rn"] = (-5.7945, -35.2110),
        ["pb"] = (-7.1195, -34.8450),
        ["al"] = (-9.6498, -35.7089),
        ["se"] = (-10.9472, -37.0731),
        ["pi"] = (-5.0892, -42.8016),
        ["ma"] = (-2.5307, -44.3068),
        ["pa"] = (-1.4558, -48.4902),
        ["am"] = (-3.1190, -60.0217),
        ["ro"] = (-8.7612, -63.9004),
        ["ac"] = (-9.9754, -67.8249),
        ["rr"] = (2.8235, -60.6758),
        ["ap"] = (0.0356, -51.0705),
        ["to"] = (-10.2491, -48.3243)
    };

    private static readonly Dictionary<string, (double Lat, double Lon)> BrazilianCityCoordinates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["blumenau|sc"] = (-26.9194, -49.0661),
        ["gaspar|sc"] = (-26.9314, -48.9586),
        ["indaial|sc"] = (-26.8978, -49.2317),
        ["pomerode|sc"] = (-26.7408, -49.1767),
        ["timbo|sc"] = (-26.8233, -49.2717),
        ["brusque|sc"] = (-27.0977, -48.9107),
        ["itajai|sc"] = (-26.9078, -48.6619),
        ["balneario camboriu|sc"] = (-26.9906, -48.6348),
        ["florianopolis|sc"] = (-27.5954, -48.5480),
        ["joinville|sc"] = (-26.3044, -48.8487),
        ["sao paulo|sp"] = (-23.5505, -46.6333),
        ["caieiras|sp"] = (-23.3644, -46.7408),
        ["franco da rocha|sp"] = (-23.3219, -46.7272),
        ["mairipora|sp"] = (-23.3186, -46.5867),
        ["campinas|sp"] = (-22.9099, -47.0626),
        ["guarulhos|sp"] = (-23.4543, -46.5337),
        ["osasco|sp"] = (-23.5325, -46.7917),
        ["santo andre|sp"] = (-23.6639, -46.5383),
        ["sao bernardo do campo|sp"] = (-23.6914, -46.5646),
        ["barueri|sp"] = (-23.5111, -46.8764),
        ["curitiba|pr"] = (-25.4284, -49.2733),
        ["porto alegre|rs"] = (-30.0346, -51.2177),
        ["rio de janeiro|rj"] = (-22.9068, -43.1729),
        ["belo horizonte|mg"] = (-19.9167, -43.9345),
        ["brasilia|df"] = (-15.7975, -47.8919),
        ["salvador|ba"] = (-12.9714, -38.5014),
        ["fortaleza|ce"] = (-3.7319, -38.5267),
        ["recife|pe"] = (-8.0476, -34.8770)
    };

    private static readonly (string Keyword, double Lat, double Lon)[] BlumenauNeighborhoods =
    {
        ("victor konder", -26.9068, -49.0772),
        ("furb", -26.9068, -49.0772),
        ("vila nova", -26.9094, -49.0867),
        ("velha", -26.9242, -49.0932),
        ("ponta aguda", -26.9147, -49.0558),
        ("garcia", -26.9355, -49.0638),
        ("itoupava norte", -26.8856, -49.0731),
        ("itoupava seca", -26.8986, -49.0778),
        ("itoupava central", -26.8260, -49.0846),
        ("salto", -26.9012, -49.1045),
        ("agua verde", -26.9178, -49.1066),
        ("escola agricola", -26.9016, -49.0928),
        ("vorstadt", -26.9225, -49.0467),
        ("boa vista", -26.9030, -49.0695),
        ("fortaleza", -26.8895, -49.0564),
        ("progresso", -26.9685, -49.0680),
        ("badenfurt", -26.8742, -49.1246),
        ("centro", -26.9194, -49.0661),
        ("xv de novembro", -26.9194, -49.0661)
    };

    public static string NormalizeGeoText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool AreCoordinatesPlausibleForLocation(double lat, double lon, string? city, string? state)
    {
        var normState = NormalizeGeoText(state);
        var normCity = NormalizeGeoText(city);
        var nearBlumenau = CalculateDistanceKm(lat, lon, -26.9194, -49.0661) < 20.0;

        if (nearBlumenau)
        {
            if (!string.IsNullOrEmpty(normState) && normState != "sc")
                return false;

            if (!string.IsNullOrEmpty(normCity) &&
                normCity is not ("blumenau" or "gaspar" or "indaial" or "pomerode"))
                return false;
        }

        return true;
    }

    public static GeoPoint ResolveAddressCoordinates(
        double? latitude,
        double? longitude,
        string? street,
        string? neighborhood,
        string? city,
        string? state,
        string? zipCode = null,
        string label = "")
    {
        if (latitude.HasValue && longitude.HasValue &&
            (Math.Abs(latitude.Value) > 0.001 || Math.Abs(longitude.Value) > 0.001) &&
            AreCoordinatesPlausibleForLocation(latitude.Value, longitude.Value, city, state))
        {
            return new GeoPoint(latitude.Value, longitude.Value, label);
        }

        var normCity = NormalizeGeoText(city);
        var normState = NormalizeGeoText(state);
        var normNeighborhood = NormalizeGeoText(neighborhood);
        var normStreet = NormalizeGeoText(street);
        var detailKey = $"{normNeighborhood}|{normStreet}|{NormalizeGeoText(zipCode)}";

        var isBlumenau = normCity == "blumenau" ||
                         (string.IsNullOrEmpty(normCity) && string.IsNullOrEmpty(normState));

        if (isBlumenau)
        {
            var combinedBlumenau = $"{normNeighborhood} {normStreet}".Trim();
            if (!string.IsNullOrEmpty(combinedBlumenau))
            {
                foreach (var (keyword, lat, lon) in BlumenauNeighborhoods)
                {
                    if (combinedBlumenau.Contains(keyword, StringComparison.Ordinal))
                        return new GeoPoint(lat, lon, label);
                }
            }

            return ApplyDeterministicOffset(-26.9194, -49.0661, detailKey, 0.012, label);
        }

        if (!string.IsNullOrEmpty(normCity))
        {
            if (!string.IsNullOrEmpty(normState) &&
                BrazilianCityCoordinates.TryGetValue($"{normCity}|{normState}", out var cityCoords))
            {
                return ApplyDeterministicOffset(cityCoords.Lat, cityCoords.Lon, detailKey, 0.008, label);
            }

            var cityMatches = BrazilianCityCoordinates
                .Where(kv => kv.Key.StartsWith($"{normCity}|", StringComparison.Ordinal))
                .ToList();
            if (cityMatches.Count == 1)
            {
                var coords = cityMatches[0].Value;
                return ApplyDeterministicOffset(coords.Lat, coords.Lon, detailKey, 0.008, label);
            }
        }

        if (!string.IsNullOrEmpty(normState) && StateCentroids.TryGetValue(normState, out var stateCenter))
        {
            var cityAnchor = ApplyDeterministicOffset(
                stateCenter.Lat,
                stateCenter.Lon,
                string.IsNullOrEmpty(normCity) ? "state-default" : normCity,
                0.12,
                label);

            return ApplyDeterministicOffset(cityAnchor.Latitude, cityAnchor.Longitude, detailKey, 0.008, label);
        }

        return ApplyDeterministicOffset(-26.9194, -49.0661, $"{normCity}|{detailKey}", 0.012, label);
    }

    public static GeoPoint ResolveAddressCoordinates(
        double? latitude,
        double? longitude,
        string? addressText,
        string label = "")
    {
        var (street, neighborhood, city, state) = ParseAddressParts(addressText);

        if (latitude.HasValue && longitude.HasValue &&
            (Math.Abs(latitude.Value) > 0.001 || Math.Abs(longitude.Value) > 0.001) &&
            AreCoordinatesPlausibleForLocation(latitude.Value, longitude.Value, city, state))
        {
            return new GeoPoint(latitude.Value, longitude.Value, label);
        }

        return ResolveAddressCoordinates(latitude, longitude, street, neighborhood, city, state, null, label);
    }

    private static (string? Street, string? Neighborhood, string? City, string? State) ParseAddressParts(string? addressText)
    {
        if (string.IsNullOrWhiteSpace(addressText))
            return (null, null, null, null);

        string? city = null;
        string? state = null;
        var cityStateMatch = Regex.Match(addressText, @"(?:,|-)\s*([^,/\-]+?)\s*/\s*([A-Za-z]{2})\b");
        if (cityStateMatch.Success)
        {
            city = cityStateMatch.Groups[1].Value.Trim();
            state = cityStateMatch.Groups[2].Value.Trim();
        }

        return (addressText, addressText, city, state);
    }

    private static GeoPoint ApplyDeterministicOffset(double baseLat, double baseLon, string key, double maxStep, string label)
    {
        if (string.IsNullOrWhiteSpace(key.Replace("|", string.Empty)))
            return new GeoPoint(Math.Round(baseLat, 5), Math.Round(baseLon, 5), label);

        var hash = GetStableHash(key);
        var stepUnit = maxStep / 30.0;
        var latOffset = ((hash % 61) - 30) * stepUnit;
        var lonOffset = (((hash / 61) % 61) - 30) * stepUnit;
        return new GeoPoint(Math.Round(baseLat + latOffset, 5), Math.Round(baseLon + lonOffset, 5), label);
    }

    private static int GetStableHash(string text)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619;
            }
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
