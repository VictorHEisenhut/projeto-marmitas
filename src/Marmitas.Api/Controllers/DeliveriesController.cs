using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Deliveries;
using Marmitas.Application.Financial;
using Marmitas.Application.Orders;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly ICreateOrderService _orders;
    private readonly IRestaurantRepository _restaurants;
    private readonly ICustomerProfileRepository _profiles;
    private readonly IDeliveryDistributionService _distribution;
    private readonly IFinancialLedgerService _ledger;

    public DeliveriesController(
        IDeliveryRepository deliveries,
        ICreateOrderService orders,
        IRestaurantRepository restaurants,
        ICustomerProfileRepository profiles,
        IDeliveryDistributionService distribution,
        IFinancialLedgerService ledger)
    {
        _deliveries = deliveries;
        _orders = orders;
        _restaurants = restaurants;
        _profiles = profiles;
        _distribution = distribution;
        _ledger = ledger;
    }

    private async Task<bool> IsCourierRegistrationCompleteAsync(Guid userId)
    {
        if (User.IsInRole("admin") || User.IsInRole("super_admin"))
            return true;

        var docs = await _deliveries.GetDocumentsByCourierAsync(userId);
        var hasCpf = docs.Any(d => string.Equals(d.Type, "CPF", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(d.Number));
        var hasCnh = docs.Any(d => string.Equals(d.Type, "CNH", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(d.Number));

        var vehicles = await _deliveries.GetVehiclesByCourierAsync(userId);
        var hasVehicle = vehicles.Any(v => !string.IsNullOrWhiteSpace(v.Model) && !string.IsNullOrWhiteSpace(v.LicensePlate));

        var courier = await _deliveries.GetCourierByUserIdAsync(userId);
        var hasBillingAddress = courier != null &&
            !string.IsNullOrWhiteSpace(courier.BillingStreet) &&
            !string.IsNullOrWhiteSpace(courier.BillingNumber) &&
            !string.IsNullOrWhiteSpace(courier.BillingNeighborhood) &&
            !string.IsNullOrWhiteSpace(courier.BillingCity) &&
            !string.IsNullOrWhiteSpace(courier.BillingState) &&
            !string.IsNullOrWhiteSpace(courier.BillingZipCode);

        return hasCpf && hasCnh && hasVehicle && hasBillingAddress;
    }

    [HttpGet("api/entregas")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetDeliveries()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        if (userId != Guid.Empty && !await IsCourierRegistrationCompleteAsync(userId))
        {
            return Ok(Array.Empty<object>());
        }

        var allOrders = await _orders.GetAllOrdersAsync();
        var existing = await _deliveries.GetAllDeliveriesAsync();
        var changed = false;

        foreach (var order in allOrders.Where(o => o.Status != "Cancelled"))
        {
            var existingDelivery = existing.FirstOrDefault(d => d.OrderId == order.Id);
            if (existingDelivery == null)
            {
                await _deliveries.AddDeliveryAsync(new Delivery
                {
                    OrderId = order.Id,
                    Status = order.Status,
                    ConfirmationPin = order.ConfirmationPin,
                    DeliveryFee = order.DeliveryFee > 0 ? order.DeliveryFee : 8.50m
                });
                changed = true;
            }
            else if (existingDelivery.CourierId == null && existingDelivery.Status != order.Status)
            {
                existingDelivery.Status = order.Status;
                changed = true;
            }
        }

        if (changed)
            await _deliveries.SaveChangesAsync();

        var updatedList = await _deliveries.GetAllDeliveriesAsync();
        var enrichedList = new List<object>();

        var ordersById = allOrders.ToDictionary(o => o.Id, o => o);
        var restaurantCache = new Dictionary<Guid, Restaurant?>();
        var customerAddressCache = new Dictionary<Guid, List<Address>>();

        foreach (var d in updatedList)
        {
            var restaurantName = "Restaurante Parceiro";
            var restaurantAddress = string.Empty;
            var deliveryNeighborhood = "Centro";
            var deliveryAddress = "Região Central - São Paulo/SP";

            if (ordersById.TryGetValue(d.OrderId, out var order))
            {
                if (!string.IsNullOrWhiteSpace(order.RestaurantName))
                {
                    restaurantName = order.RestaurantName;
                }

                if (!restaurantCache.TryGetValue(order.RestaurantId, out var rest))
                {
                    rest = await _restaurants.GetByIdAsync(order.RestaurantId);
                    restaurantCache[order.RestaurantId] = rest;
                }

                if (rest != null)
                {
                    restaurantName = rest.Name;
                    if (!string.IsNullOrWhiteSpace(rest.AddressStreet))
                    {
                        restaurantAddress = $"{rest.AddressStreet}, {rest.AddressNumber} - {rest.AddressNeighborhood}, {rest.AddressCity}/{rest.AddressState}";
                    }
                }

                if (!customerAddressCache.TryGetValue(order.CustomerId, out var addrs))
                {
                    addrs = await _profiles.GetAddressesByUserAsync(order.CustomerId);
                    customerAddressCache[order.CustomerId] = addrs;
                }

                var mainAddr = addrs.FirstOrDefault(a => a.IsMain) ?? addrs.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(order.DeliveryAddress))
                {
                    deliveryAddress = order.DeliveryAddress;
                    deliveryNeighborhood = mainAddr?.Neighborhood ?? "Entrega";
                }
                else if (mainAddr != null && !string.IsNullOrWhiteSpace(mainAddr.Neighborhood))
                {
                    deliveryNeighborhood = mainAddr.Neighborhood;
                    deliveryAddress = $"{mainAddr.Street}, {mainAddr.Number} - {mainAddr.Neighborhood}, {mainAddr.City}/{mainAddr.State}";
                }
                else if (rest != null && !string.IsNullOrWhiteSpace(rest.AddressNeighborhood))
                {
                    deliveryNeighborhood = rest.AddressNeighborhood;
                    deliveryAddress = $"Região {rest.AddressNeighborhood} - {rest.AddressCity}/{rest.AddressState}";
                }
            }

            enrichedList.Add(new
            {
                id = d.Id,
                orderId = d.OrderId,
                courierId = d.CourierId,
                status = d.Status,
                deliveryFee = d.DeliveryFee > 0 ? d.DeliveryFee : 8.50m,
                distanceKm = d.DistanceKm ?? 3.4,
                createdAt = d.CreatedAt,
                updatedAt = d.UpdatedAt,
                restaurantName,
                restaurantAddress,
                deliveryNeighborhood,
                deliveryAddress
            });
        }

        return Ok(enrichedList);
    }

    [HttpGet("api/entregas/{id}")]
    [Authorize(Roles = "customer,driver,admin,super_admin")]
    public async Task<IActionResult> GetDelivery(Guid id)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery
            {
                OrderId = order.Id,
                Status = order.Status,
                ConfirmationPin = order.ConfirmationPin
            };
            await _deliveries.AddDeliveryAsync(delivery);
            await _deliveries.SaveChangesAsync();
        }

        return Ok(delivery);
    }

    [HttpGet("api/entregas/{id}/candidatos")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetDeliveryCandidates(Guid id)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        var orderId = delivery?.OrderId ?? id;
        var order = await _orders.GetByIdAsync(orderId);
        var restaurant = await _restaurants.GetByIdAsync(order.RestaurantId);

        var lat = restaurant?.Latitude ?? -23.5614;
        var lon = restaurant?.Longitude ?? -46.6559;
        var ranked = await _distribution.RankAvailableCouriersAsync(lat, lon);

        return Ok(ranked);
    }

    [HttpPatch("api/entregas/{id}/aceitar")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> AcceptDelivery(Guid id)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        if (userId != Guid.Empty && !await IsCourierRegistrationCompleteAsync(userId))
        {
            throw new Exception("Complete seu cadastro obrigatório (CPF, CNH, Veículo e Endereço de Cobrança) antes de aceitar entregas.");
        }

        var courier = await _deliveries.GetCourierByUserIdAsync(userId);
        if (courier != null && string.Equals(courier.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Seu cadastro de entregador não está aprovado para aceitar entregas.");
        }

        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery
            {
                OrderId = order.Id,
                ConfirmationPin = order.ConfirmationPin
            };
            await _deliveries.AddDeliveryAsync(delivery);
        }

        delivery.AssignCourier(userId);
        await _deliveries.SaveChangesAsync();
        await _orders.UpdateStatusAsync(delivery.OrderId, "DriverAssigned", userId, "driver", "Entrega aceita pelo entregador");

        return Ok(delivery);
    }

    [HttpPatch("api/entregas/{id}/status")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery
            {
                OrderId = order.Id,
                ConfirmationPin = string.IsNullOrWhiteSpace(order.ConfirmationPin) ? "1234" : order.ConfirmationPin
            };
            await _deliveries.AddDeliveryAsync(delivery);
        }
        else if (string.IsNullOrWhiteSpace(delivery.ConfirmationPin))
        {
            var order = await _orders.GetByIdAsync(delivery.OrderId);
            delivery.ConfirmationPin = string.IsNullOrWhiteSpace(order.ConfirmationPin) ? "1234" : order.ConfirmationPin;
        }

        var isAdminOverride = User.IsInRole("admin") || User.IsInRole("super_admin");
        delivery.UpdateStatus(request.Status, request.ConfirmationPin, isAdminOverride);
        await _deliveries.SaveChangesAsync();
        await _orders.UpdateStatusAsync(delivery.OrderId, request.Status, userId, "driver", $"Status da entrega atualizado para {request.Status}");

        if (request.Status == "Delivered")
        {
            var courierId = delivery.CourierId ?? userId;
            await _ledger.RecordDeliveryCompletedAsync(delivery, courierId);
            await _deliveries.SaveChangesAsync();
        }

        return Ok(delivery);
    }

    [HttpGet("api/entregas/{id}/rastreamento")]
    [Authorize(Roles = "customer,driver,admin,super_admin")]
    public async Task<IActionResult> TrackDelivery(Guid id)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        Courier? courier = null;
        List<DriverLocation> history = new();

        if (delivery?.CourierId != null)
        {
            courier = await _deliveries.GetCourierByUserIdAsync(delivery.CourierId.Value);
            history = await _deliveries.GetDriverLocationsAsync(delivery.CourierId.Value, 10);
        }

        return Ok(new
        {
            deliveryId = delivery?.Id ?? id,
            orderId = delivery?.OrderId ?? id,
            status = delivery?.Status ?? "InTransit",
            courierName = courier?.Name,
            latitude = courier?.Latitude ?? -23.5629,
            longitude = courier?.Longitude ?? -46.6544,
            recentLocations = history.Select(h => new
            {
                h.Latitude,
                h.Longitude,
                h.RecordedAt
            }),
            updatedAt = delivery?.UpdatedAt ?? DateTime.UtcNow
        });
    }

    [HttpPatch("api/entregadores/me/disponibilidade")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> SetAvailability([FromBody] UpdateCourierAvailabilityRequest request)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId))
        {
            var courier = await _deliveries.GetCourierByUserIdAsync(userId);
            if (courier != null)
            {
                courier.IsAvailable = request.IsAvailable;
                await _deliveries.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            isAvailable = request.IsAvailable,
            updatedAt = DateTime.UtcNow
        });
    }
}
