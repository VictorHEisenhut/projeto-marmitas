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

    public static GeoPoint ResolveAddressCoordinates(
        double? latitude,
        double? longitude,
        string? addressText,
        string label = "")
    {
        if (latitude.HasValue && longitude.HasValue &&
            (Math.Abs(latitude.Value) > 0.001 || Math.Abs(longitude.Value) > 0.001))
        {
            return new GeoPoint(latitude.Value, longitude.Value, label);
        }

        var text = (addressText ?? string.Empty).ToLowerInvariant();
        if (text.Contains("victor konder") || text.Contains("furb")) return new GeoPoint(-26.9068, -49.0772, label);
        if (text.Contains("vila nova")) return new GeoPoint(-26.9094, -49.0867, label);
        if (text.Contains("velha")) return new GeoPoint(-26.9242, -49.0932, label);
        if (text.Contains("ponta aguda")) return new GeoPoint(-26.9147, -49.0558, label);
        if (text.Contains("garcia")) return new GeoPoint(-26.9355, -49.0638, label);
        if (text.Contains("itoupava norte")) return new GeoPoint(-26.8856, -49.0731, label);
        if (text.Contains("itoupava seca")) return new GeoPoint(-26.8986, -49.0778, label);
        if (text.Contains("itoupava central")) return new GeoPoint(-26.8260, -49.0846, label);
        if (text.Contains("salto")) return new GeoPoint(-26.9012, -49.1045, label);
        if (text.Contains("água verde") || text.Contains("agua verde")) return new GeoPoint(-26.9178, -49.1066, label);
        if (text.Contains("escola agrícola") || text.Contains("escola agricola")) return new GeoPoint(-26.9016, -49.0928, label);
        if (text.Contains("vorstadt")) return new GeoPoint(-26.9225, -49.0467, label);
        if (text.Contains("boa vista")) return new GeoPoint(-26.9030, -49.0695, label);
        if (text.Contains("fortaleza")) return new GeoPoint(-26.8895, -49.0564, label);
        if (text.Contains("progresso")) return new GeoPoint(-26.9685, -49.0680, label);
        if (text.Contains("badenfurt")) return new GeoPoint(-26.8742, -49.1246, label);
        if (text.Contains("centro") || text.Contains("xv de novembro")) return new GeoPoint(-26.9194, -49.0661, label);

        // Deslocamento determinístico dentro da zona urbana de Blumenau/SC quando o endereço não possui geocodificação prévia
        var hash = Math.Abs((addressText ?? "default").GetHashCode());
        var latOffset = ((hash % 60) - 30) * 0.0012;
        var lonOffset = (((hash / 60) % 60) - 30) * 0.0012;
        return new GeoPoint(Math.Round(-26.9194 + latOffset, 5), Math.Round(-49.0661 + lonOffset, 5), label);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
