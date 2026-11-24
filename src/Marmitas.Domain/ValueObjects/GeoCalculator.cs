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
        if (text.Contains("vila madalena")) return new GeoPoint(-23.5535, -46.6902, label);
        if (text.Contains("moema")) return new GeoPoint(-23.6022, -46.6619, label);
        if (text.Contains("bela vista") || text.Contains("paulista")) return new GeoPoint(-23.5594, -46.6468, label);
        if (text.Contains("pinheiros")) return new GeoPoint(-23.5661, -46.6805, label);
        if (text.Contains("vila mariana")) return new GeoPoint(-23.5890, -46.6365, label);
        if (text.Contains("itaim")) return new GeoPoint(-23.5847, -46.6783, label);
        if (text.Contains("jardins") || text.Contains("jardim")) return new GeoPoint(-23.5649, -46.6689, label);
        if (text.Contains("brooklin")) return new GeoPoint(-23.6114, -46.6915, label);
        if (text.Contains("perdizes")) return new GeoPoint(-23.5364, -46.6781, label);
        if (text.Contains("consolação") || text.Contains("consolacao")) return new GeoPoint(-23.5530, -46.6606, label);
        if (text.Contains("tatuapé") || text.Contains("tatuape")) return new GeoPoint(-23.5407, -46.5766, label);
        if (text.Contains("santana")) return new GeoPoint(-23.5015, -46.6252, label);
        if (text.Contains("morumbi")) return new GeoPoint(-23.5975, -46.7166, label);
        if (text.Contains("centro") || text.Contains("sé") || text.Contains("republica")) return new GeoPoint(-23.5505, -46.6333, label);

        // Deslocamento determinístico dentro da zona urbana quando o endereço não possui geocodificação prévia
        var hash = Math.Abs((addressText ?? "default").GetHashCode());
        var latOffset = ((hash % 60) - 30) * 0.0012;
        var lonOffset = (((hash / 60) % 60) - 30) * 0.0012;
        return new GeoPoint(Math.Round(-23.5614 + latOffset, 5), Math.Round(-46.6559 + lonOffset, 5), label);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
