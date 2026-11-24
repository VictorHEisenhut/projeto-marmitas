using System.Text.Json;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;
using Marmitas.Domain.ValueObjects;

namespace Marmitas.Application.Orders;

public interface ICreateOrderService
{
    Task<CreateOrderResponse> ExecuteAsync(Guid customerId, CreateOrderRequest request);
    Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId);
    Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto> GetByIdAsync(Guid id);
    Task<List<OrderItemDto>> GetItemsAsync(Guid id);
    Task<List<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id);
    Task<OrderDto> CancelAsync(Guid id, bool isAdminOrStaff = false);
    Task<OrderDto> UpdateStatusAsync(Guid id, string status, Guid? actorUserId = null, string? actorRole = null, string? notes = null);
    Task<OrderDto> SubmitComplaintAsync(Guid id, string message);
    Task<OrderDto> ReplyComplaintAsync(Guid id, string reply);
}

public class CreateOrderService : ICreateOrderService
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISubscriptionRepository? _subscriptionRepository;
    private readonly ICustomerProfileRepository? _profileRepository;
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public CreateOrderService(
        IRestaurantRepository restaurantRepository,
        IOrderRepository orderRepository,
        ISubscriptionRepository? subscriptionRepository = null,
        ICustomerProfileRepository? profileRepository = null,
        IRealtimeNotifier? realtimeNotifier = null)
    {
        _restaurantRepository = restaurantRepository;
        _orderRepository = orderRepository;
        _subscriptionRepository = subscriptionRepository;
        _profileRepository = profileRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<CreateOrderResponse> ExecuteAsync(Guid customerId, CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            throw new Exception("O pedido deve conter pelo menos um item.");

        if (request.Items.Any(i => i.Quantity <= 0))
            throw new Exception("A quantidade dos itens deve ser maior que zero.");

        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId);
        if (restaurant == null || restaurant.Status != RestaurantStatus.Approved)
            throw new Exception("Restaurante não encontrado ou inativo.");

        var restaurantHours = await _restaurantRepository.GetRestaurantHoursAsync(request.RestaurantId);
        if (restaurantHours.Count > 0)
        {
            var currentDay = (int)DateTime.UtcNow.DayOfWeek;
            var todayRule = restaurantHours.FirstOrDefault(h => h.DayOfWeek == currentDay);
            if (todayRule is { IsOpen: false })
                throw new Exception("O restaurante está fechado para pedidos no dia de hoje.");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var activeProducts = await _restaurantRepository.GetActiveProductsByIdsAsync(request.RestaurantId, productIds);

        if (activeProducts.Count != productIds.Count)
            throw new Exception("Um ou mais produtos não existem, não pertencem ao restaurante ou estão inativos.");

        Subscription? activeSub = null;
        if (request.IsPlanRedemption && _subscriptionRepository != null)
        {
            var userSubs = await _subscriptionRepository.GetSubscriptionsByUserAsync(customerId);
            activeSub = userSubs.FirstOrDefault(s =>
                string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                (s.RestaurantId == request.RestaurantId ||
                 (s.RestaurantId == Guid.Empty && s.PreferredRestaurantId == request.RestaurantId)));

            if (activeSub == null)
                throw new Exception($"Você não possui um plano de assinatura ativo no restaurante '{restaurant.Name}' para resgatar marmitas.");

            var plan = await _subscriptionRepository.GetPlanByIdAsync(activeSub.PlanId);
            var weeklyLimit = plan?.MealsPerWeek ?? 5;

            var myOrders = await _orderRepository.GetByCustomerIdAsync(customerId);
            var weekCutoff = DateTime.UtcNow.AddDays(-7);
            var usedMealsThisWeek = myOrders
                .Where(o => o.Status != OrderStatus.Cancelled
                            && o.CreatedAt >= weekCutoff
                            && (o.SubscriptionId == activeSub.Id || o.RestaurantId == request.RestaurantId))
                .SelectMany(o => o.Items)
                .Where(i => i.ProductName.StartsWith("[PLANO]", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Quantity);

            var requestedMeals = request.Items.Sum(i => i.Quantity);
            if (usedMealsThisWeek + requestedMeals > weeklyLimit)
            {
                var remaining = Math.Max(0, weeklyLimit - usedMealsThisWeek);
                throw new Exception(
                    $"Limite semanal do seu plano atingido ({weeklyLimit} marmitas/semana). Você já utilizou {usedMealsThisWeek} e possui {remaining} disponível(is) nesta semana.");
            }

            activeSub.UsedMealsInCycle += requestedMeals;
        }

        var order = new Order(customerId, request.RestaurantId);
        if (activeSub != null)
        {
            order.SubscriptionId = activeSub.Id;
        }

        if (_profileRepository != null)
        {
            Address? resolvedAddress;
            if (request.AddressId.HasValue && request.AddressId.Value != Guid.Empty)
            {
                resolvedAddress = await _profileRepository.GetAddressByIdAsync(request.AddressId.Value, customerId);
                if (resolvedAddress == null)
                    throw new Exception("Endereço de entrega não encontrado ou não pertence à sua conta. Cadastre e selecione um endereço válido.");
            }
            else
            {
                var addrs = await _profileRepository.GetAddressesByUserAsync(customerId);
                resolvedAddress = addrs.FirstOrDefault(a => a.IsMain) ?? addrs.FirstOrDefault();
            }

            if (resolvedAddress == null)
                throw new Exception("É obrigatório cadastrar um endereço de entrega antes de fazer um pedido.");

            if (!string.IsNullOrWhiteSpace(restaurant.AddressCity) ||
                !string.IsNullOrWhiteSpace(restaurant.AddressState) ||
                (restaurant.Latitude.HasValue && restaurant.Longitude.HasValue))
            {
                var restPoint = GeoCalculator.ResolveAddressCoordinates(
                    restaurant.Latitude,
                    restaurant.Longitude,
                    restaurant.AddressStreet,
                    restaurant.AddressNeighborhood,
                    restaurant.AddressCity,
                    restaurant.AddressState,
                    restaurant.AddressZipCode,
                    restaurant.Name);
                var custPoint = GeoCalculator.ResolveAddressCoordinates(
                    resolvedAddress.Latitude,
                    resolvedAddress.Longitude,
                    resolvedAddress.Street,
                    resolvedAddress.Neighborhood,
                    resolvedAddress.City,
                    resolvedAddress.State,
                    resolvedAddress.ZipCode,
                    "Cliente");

                var distanceKm = GeoCalculator.CalculateDistanceKm(
                    restPoint.Latitude,
                    restPoint.Longitude,
                    custPoint.Latitude,
                    custPoint.Longitude);
                var maxRadius = restaurant.DeliveryRadiusKm > 0 ? restaurant.DeliveryRadiusKm : 8.0;
                if (distanceKm > maxRadius)
                {
                    throw new Exception(
                        $"Seu endereço ({resolvedAddress.City}/{resolvedAddress.State}) está a {distanceKm:F1} km e fora do raio de entrega de {maxRadius:F0} km do restaurante '{restaurant.Name}'.");
                }
            }

            order.DeliveryAddress = FormatAddress(resolvedAddress);
        }
        else if (!string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            order.DeliveryAddress = request.DeliveryAddress.Trim();
        }
        else
        {
            throw new Exception("É obrigatório cadastrar um endereço de entrega antes de fazer um pedido.");
        }

        foreach (var itemRequest in request.Items)
        {
            var product = activeProducts.First(p => p.Id == itemRequest.ProductId);

            if (product.DailyStock.HasValue && product.DailyStock.Value < itemRequest.Quantity)
            {
                throw new Exception($"Estoque diário insuficiente para a marmita '{product.Name}'.");
            }

            if (request.IsPlanRedemption && !product.AvailableInPlans)
            {
                throw new Exception($"A marmita '{product.Name}' não está disponível para resgate no plano mensal.");
            }

            if (itemRequest.WantsCutlery && !product.AllowsCutlery)
            {
                throw new Exception($"O prato '{product.Name}' não permite opção de talheres.");
            }

            var cutleryCost = itemRequest.WantsCutlery && product.AllowsCutlery && product.CutleryHasExtraCost
                ? product.CutleryPrice
                : 0m;

            var productAddons = DeserializeAddons(product.AddonsJson);
            var addonsCost = 0m;
            var detailsSuffix = new List<string>();

            if (itemRequest.SelectedAddons != null)
            {
                foreach (var selected in itemRequest.SelectedAddons.Where(a => a.Quantity > 0))
                {
                    var match = productAddons.FirstOrDefault(a =>
                        string.Equals(a.Name.Trim(), selected.Name.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                        throw new Exception($"Adicional '{selected.Name}' não existe para a marmita '{product.Name}'.");

                    if (selected.Quantity > match.MaxQuantity)
                        throw new Exception($"O adicional '{match.Name}' permite no máximo {match.MaxQuantity} unidade(s) por marmita.");

                    addonsCost += match.ExtraPrice * selected.Quantity;
                    detailsSuffix.Add($"{selected.Quantity}x {match.Name}");
                }
            }

            if (itemRequest.WantsCutlery && product.AllowsCutlery)
            {
                detailsSuffix.Add(cutleryCost > 0 ? "Com talher (extra)" : "Com talher (grátis)");
            }

            var basePrice = request.IsPlanRedemption ? 0m : product.GetEffectivePrice();
            var finalUnitPrice = basePrice + addonsCost + cutleryCost;

            var itemDisplayName = detailsSuffix.Count > 0
                ? $"{product.Name} ({string.Join(", ", detailsSuffix)})"
                : product.Name;

            if (request.IsPlanRedemption)
            {
                itemDisplayName = $"[PLANO] {itemDisplayName}";
            }

            var orderItem = new OrderItem(product.Id, itemDisplayName, finalUnitPrice, itemRequest.Quantity);
            order.AddItem(orderItem);
        }

        if (!request.IsPlanRedemption)
        {
            var zones = await _restaurantRepository.GetDeliveryZonesAsync(request.RestaurantId);
            var matchedZone = zones.FirstOrDefault(z =>
                !string.IsNullOrWhiteSpace(order.DeliveryAddress) &&
                order.DeliveryAddress.Contains(z.Neighborhood, StringComparison.OrdinalIgnoreCase));

            if (matchedZone != null)
            {
                order.SetDeliveryFee(matchedZone.DeliveryFee);
            }
            else if (restaurant.DeliveryFee > 0)
            {
                order.SetDeliveryFee(restaurant.DeliveryFee);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CouponCode) && _subscriptionRepository != null)
        {
            var coupon = await _subscriptionRepository.GetCouponByCodeAsync(request.CouponCode);
            if (coupon == null || coupon.ExpiresAt < DateTime.UtcNow)
                throw new Exception("O cupom informado é inválido ou expirou.");

            order.ApplyCoupon(coupon.Code, coupon.DiscountPercentage);
            await _subscriptionRepository.AddCouponUsageAsync(new CouponUsage
            {
                CouponId = coupon.Id,
                UserId = customerId,
                UsedAt = DateTime.UtcNow
            });
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = "None",
            ToStatus = order.Status.ToString(),
            ChangedByUserId = customerId,
            ChangedByRole = "customer",
            Notes = "Pedido criado"
        });

        await CreateNotificationSafeAsync(
            customerId,
            "Pedido recebido!",
            $"Seu pedido #{order.Id.ToString()[..8]} no restaurante {restaurant.Name} foi criado ({order.TotalAmount:C2}). Código de entrega: {order.ConfirmationPin}.");

        await _orderRepository.SaveChangesAsync();

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyOrderStatusChangedAsync(order.Id, customerId, order.RestaurantId, order.Status.ToString());
        }

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.CouponCode,
            DeliveryAddress = order.DeliveryAddress,
            ConfirmationPin = order.ConfirmationPin,
            Status = order.Status.ToString()
        };
    }

    private static string FormatAddress(Address addr)
    {
        var comp = string.IsNullOrWhiteSpace(addr.Complement) ? string.Empty : $" ({addr.Complement})";
        return $"{addr.Street}, {addr.Number}{comp} - {addr.Neighborhood}, {addr.City}/{addr.State} - CEP {addr.ZipCode}";
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId)
    {
        var orders = await _orderRepository.GetByRestaurantIdAsync(restaurantId);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapOrder).ToList();
    }

    public async Task<OrderDto> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        return MapOrder(order);
    }

    public async Task<List<OrderItemDto>> GetItemsAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        return order.Items.Select(MapItem).ToList();
    }

    public async Task<List<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        var history = await _orderRepository.GetStatusHistoryAsync(id);
        if (history.Count == 0)
        {
            return new List<OrderStatusHistoryDto>
            {
                new()
                {
                    Id = order.Id,
                    OrderId = order.Id,
                    FromStatus = "Created",
                    ToStatus = order.Status.ToString(),
                    CreatedAt = order.UpdatedAt
                }
            };
        }

        return history.Select(h => new OrderStatusHistoryDto
        {
            Id = h.Id,
            OrderId = h.OrderId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByRole = h.ChangedByRole,
            Notes = h.Notes,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    public async Task<OrderDto> CancelAsync(Guid id, bool isAdminOrStaff = false)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (order.Status == OrderStatus.Delivered)
            throw new Exception("Não é possível cancelar um pedido já entregue.");

        if (!isAdminOrStaff && !order.CanBeCancelledByCustomer())
            throw new Exception("O pedido já entrou em preparo/transporte e não pode ser cancelado diretamente pelo cliente.");

        var previous = order.Status.ToString();
        order.TransitionTo(OrderStatus.Cancelled);

        if (order.SubscriptionId.HasValue && _subscriptionRepository != null)
        {
            var sub = await _subscriptionRepository.GetSubscriptionByIdAsync(order.SubscriptionId.Value);
            if (sub != null)
            {
                var planMeals = order.Items
                    .Where(i => i.ProductName.StartsWith("[PLANO]", StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.Quantity);
                sub.UsedMealsInCycle = Math.Max(0, sub.UsedMealsInCycle - planMeals);
            }
        }

        await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previous,
            ToStatus = OrderStatus.Cancelled.ToString(),
            ChangedByRole = isAdminOrStaff ? "admin" : "customer",
            Notes = "Pedido cancelado"
        });

        await CreateNotificationSafeAsync(
            order.CustomerId,
            "Pedido cancelado",
            $"Seu pedido #{order.Id.ToString()[..8]} foi cancelado.");

        await _orderRepository.SaveChangesAsync();

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyOrderStatusChangedAsync(order.Id, order.CustomerId, order.RestaurantId, order.Status.ToString());
        }

        return MapOrder(order);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, string status, Guid? actorUserId = null, string? actorRole = null, string? notes = null)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            throw new Exception("Status de pedido inválido.");

        var previous = order.Status.ToString();
        order.TransitionTo(parsedStatus);

        await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previous,
            ToStatus = parsedStatus.ToString(),
            ChangedByUserId = actorUserId,
            ChangedByRole = actorRole,
            Notes = notes
        });

        var (notifTitle, notifMsg) = GetStatusNotificationMessage(order.Id, parsedStatus);
        await CreateNotificationSafeAsync(order.CustomerId, notifTitle, notifMsg);

        await _orderRepository.SaveChangesAsync();

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyOrderStatusChangedAsync(order.Id, order.CustomerId, order.RestaurantId, parsedStatus.ToString());
        }

        return MapOrder(order);
    }

    private async Task CreateNotificationSafeAsync(Guid userId, string title, string message)
    {
        if (_profileRepository == null) return;
        await _profileRepository.AddNotificationAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false
        });

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyUserAsync(userId, title, message);
        }
    }

    private static (string Title, string Message) GetStatusNotificationMessage(Guid orderId, OrderStatus status)
    {
        var code = orderId.ToString()[..8];
        return status switch
        {
            OrderStatus.Paid => ("Pagamento aprovado", $"O pagamento do pedido #{code} foi aprovado."),
            OrderStatus.Confirmed => ("Pedido confirmado", $"O restaurante confirmou seu pedido #{code}."),
            OrderStatus.Preparing => ("Pedido em preparo", $"Sua marmita do pedido #{code} está sendo preparada."),
            OrderStatus.ReadyForPickup => ("Pedido pronto", $"Seu pedido #{code} está pronto aguardando coleta."),
            OrderStatus.DriverAssigned => ("Entregador atribuído", $"Um entregador aceitou a coleta do seu pedido #{code}."),
            OrderStatus.PickedUp => ("Pedido coletado", $"O entregador retirou seu pedido #{code} no restaurante."),
            OrderStatus.InTransit => ("Pedido em trânsito", $"Seu pedido #{code} saiu para entrega no seu endereço."),
            OrderStatus.Delivered => ("Pedido entregue", $"Seu pedido #{code} foi entregue. Bom apetite!"),
            OrderStatus.Cancelled => ("Pedido cancelado", $"O pedido #{code} foi cancelado."),
            OrderStatus.Refunded => ("Pedido reembolsado", $"O reembolso do pedido #{code} foi processado."),
            OrderStatus.Failed => ("Pagamento recusado", $"Houve uma falha no pagamento do pedido #{code}."),
            _ => ("Atualização do pedido", $"O status do seu pedido #{code} mudou para {status}.")
        };
    }

    public async Task<OrderDto> SubmitComplaintAsync(Guid id, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new Exception("Informe a descrição da reclamação.");

        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        order.ComplaintMessage = message.Trim();
        order.ComplaintCreatedAt = DateTime.UtcNow;
        await _orderRepository.SaveChangesAsync();

        return MapOrder(order);
    }

    public async Task<OrderDto> ReplyComplaintAsync(Guid id, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            throw new Exception("Informe a resposta para a reclamação do cliente.");

        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        order.ComplaintReply = reply.Trim();
        order.ComplaintRepliedAt = DateTime.UtcNow;
        await CreateNotificationSafeAsync(
            order.CustomerId,
            "Resposta à sua reclamação",
            $"O restaurante respondeu à sua reclamação no pedido #{order.Id.ToString()[..8]}.");
        await _orderRepository.SaveChangesAsync();

        return MapOrder(order);
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer != null ? o.Customer.Name : "Cliente",
        RestaurantId = o.RestaurantId,
        RestaurantName = o.Restaurant != null ? o.Restaurant.Name : string.Empty,
        SubscriptionId = o.SubscriptionId,
        Subtotal = o.Subtotal > 0 ? o.Subtotal : o.Items.Sum(i => i.TotalPrice),
        DeliveryFee = o.DeliveryFee,
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.DiscountAmount,
        CouponCode = o.CouponCode,
        DeliveryAddress = o.DeliveryAddress,
        ConfirmationPin = o.ConfirmationPin,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        ComplaintMessage = o.ComplaintMessage,
        ComplaintCreatedAt = o.ComplaintCreatedAt,
        ComplaintReply = o.ComplaintReply,
        ComplaintRepliedAt = o.ComplaintRepliedAt,
        Items = o.Items.Select(MapItem).ToList()
    };

    private static List<ProductAddonDto> DeserializeAddons(string? addonsJson)
    {
        if (string.IsNullOrWhiteSpace(addonsJson)) return new List<ProductAddonDto>();
        try
        {
            return JsonSerializer.Deserialize<List<ProductAddonDto>>(addonsJson) ?? new List<ProductAddonDto>();
        }
        catch
        {
            return new List<ProductAddonDto>();
        }
    }

    private static OrderItemDto MapItem(OrderItem i) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ProductName = i.ProductName,
        UnitPrice = i.UnitPrice,
        Quantity = i.Quantity,
        TotalPrice = i.TotalPrice
    };
}
