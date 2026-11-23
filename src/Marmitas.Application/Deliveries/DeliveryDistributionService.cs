using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Marmitas.Domain.ValueObjects;

namespace Marmitas.Application.Deliveries;

public record CourierCandidateScore(
    Guid CourierId,
    string CourierName,
    string VehicleType,
    double DistanceKm,
    int ActiveDeliveries,
    double Score);

public interface IDeliveryDistributionService
{
    Task<List<CourierCandidateScore>> RankAvailableCouriersAsync(
        double restaurantLat,
        double restaurantLon);
}

public class DeliveryDistributionService : IDeliveryDistributionService
{
    private readonly IDeliveryRepository _deliveries;

    public DeliveryDistributionService(IDeliveryRepository deliveries)
    {
        _deliveries = deliveries;
    }

    public async Task<List<CourierCandidateScore>> RankAvailableCouriersAsync(
        double restaurantLat,
        double restaurantLon)
    {
        var couriers = await _deliveries.GetAllCouriersAsync();
        var allDeliveries = await _deliveries.GetAllDeliveriesAsync();
        var results = new List<CourierCandidateScore>();

        foreach (var courier in couriers.Where(c => c.IsAvailable && string.Equals(c.Status, "Approved", StringComparison.OrdinalIgnoreCase)))
        {
            var courierLat = courier.Latitude ?? -23.5614;
            var courierLon = courier.Longitude ?? -46.6559;
            var distanceKm = GeoCalculator.CalculateDistanceKm(restaurantLat, restaurantLon, courierLat, courierLon);

            var activeCount = allDeliveries.Count(d =>
                (d.CourierId == courier.Id || d.CourierId == courier.UserId) &&
                d.Status is "DriverAssigned" or "PickedUp" or "InTransit");

            if (activeCount >= 3)
                continue;

            var vehicles = await _deliveries.GetVehiclesByCourierAsync(courier.Id);
            if (vehicles.Count == 0 && courier.UserId.HasValue)
            {
                vehicles = await _deliveries.GetVehiclesByCourierAsync(courier.UserId.Value);
            }

            var vehicleType = vehicles.FirstOrDefault()?.Type ?? "Moto";
            if (string.Equals(vehicleType, "Bicicleta", StringComparison.OrdinalIgnoreCase) && distanceKm > 6.0)
                continue;

            var distanceScore = Math.Max(0, 100.0 - distanceKm * 8.0);
            var loadPenalty = activeCount * 25.0;
            var vehicleBonus = string.Equals(vehicleType, "Moto", StringComparison.OrdinalIgnoreCase) ? 10.0 : 5.0;
            var finalScore = Math.Round(Math.Max(0, distanceScore - loadPenalty + vehicleBonus), 2);

            results.Add(new CourierCandidateScore(
                courier.Id,
                courier.Name,
                vehicleType,
                distanceKm,
                activeCount,
                finalScore));
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.DistanceKm).ToList();
    }
}
