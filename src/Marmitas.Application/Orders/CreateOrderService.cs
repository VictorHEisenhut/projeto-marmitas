using System.Text.Json;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;

namespace Marmitas.Application.Orders;

public interface ICreateOrderService
{
    Task<CreateOrderResponse> ExecuteAsync(Guid customerId, CreateOrderRequest request);
    Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto> GetByIdAsync(Guid id);
    Task<List<OrderItemDto>> GetItemsAsync(Guid id);
    Task<OrderDto> CancelAsync(Guid id);
    Task<OrderDto> UpdateStatusAsync(Guid id, string status);
    Task<OrderDto> SubmitComplaintAsync(Guid id, string message);
    Task<OrderDto> ReplyComplaintAsync(Guid id, string reply);
}

public class CreateOrderService : ICreateOrderService
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISubscriptionRepository? _subscriptionRepository;
    private readonly ICustomerProfileRepository? _profileRepository;

    public CreateOrderService(
        IRestaurantRepository restaurantRepository,
        IOrderRepository orderRepository,
        ISubscriptionRepository? subscriptionRepository = null,
        ICustomerProfileRepository? profileRepository = null)
    {
        _restaurantRepository = restaurantRepository;
        _orderRepository = orderRepository;
        _subscriptionRepository = subscriptionRepository;
        _profileRepository = profileRepository;
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

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var activeProducts = await _restaurantRepository.GetActiveProductsByIdsAsync(request.RestaurantId, productIds);

        if (activeProducts.Count() != productIds.Count)
            throw new Exception("Um ou mais produtos não existem, não pertencem ao restaurante ou estão inativos.");

        if (request.IsPlanRedemption && _subscriptionRepository != null)
        {
            var userSubs = await _subscriptionRepository.GetSubscriptionsByUserAsync(customerId);
            var activeSub = userSubs.FirstOrDefault(s =>
                string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase));

            if (activeSub == null)
                throw new Exception("Você não possui um plano de assinatura ativo para resgatar marmitas.");

            var plan = await _subscriptionRepository.GetPlanByIdAsync(activeSub.PlanId);
            var weeklyLimit = plan?.MealsPerWeek ?? 5;

            var myOrders = await _orderRepository.GetByCustomerIdAsync(customerId);
            var weekCutoff = DateTime.UtcNow.AddDays(-7);
            var usedMealsThisWeek = myOrders
                .Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= weekCutoff)
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
        }

        var order = new Order(customerId, request.RestaurantId);

        if (!string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            order.DeliveryAddress = request.DeliveryAddress.Trim();
        }
        else if (request.AddressId.HasValue && request.AddressId.Value != Guid.Empty && _profileRepository != null)
        {
            var addr = await _profileRepository.GetAddressByIdAsync(request.AddressId.Value, customerId);
            if (addr != null)
            {
                order.DeliveryAddress = FormatAddress(addr);
            }
        }
        else if (_profileRepository != null)
        {
            var addrs = await _profileRepository.GetAddressesByUserAsync(customerId);
            var mainAddr = addrs.FirstOrDefault(a => a.IsMain) ?? addrs.FirstOrDefault();
            if (mainAddr != null)
            {
                order.DeliveryAddress = FormatAddress(mainAddr);
            }
        }

        foreach (var itemRequest in request.Items)
        {
            var product = activeProducts.First(p => p.Id == itemRequest.ProductId);

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

            var basePrice = request.IsPlanRedemption ? 0m : product.Price;
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
        if (_profileRepository != null)
        {
            await _profileRepository.AddNotificationAsync(new Notification
            {
                UserId = customerId,
                Title = "Pedido confirmado!",
                Message = $"Seu pedido no restaurante {restaurant.Name} foi recebido ({order.TotalAmount:C2}).",
                IsRead = false
            });
        }
        await _orderRepository.SaveChangesAsync();

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.CouponCode,
            DeliveryAddress = order.DeliveryAddress,
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

    public async Task<OrderDto> CancelAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (order.Status == OrderStatus.Delivered)
            throw new Exception("Não é possível cancelar um pedido já entregue.");

        order.Status = OrderStatus.Cancelled;
        await _orderRepository.SaveChangesAsync();

        return MapOrder(order);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, string status)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            throw new Exception("Status de pedido inválido.");

        order.Status = parsedStatus;
        if (_profileRepository != null)
        {
            await _profileRepository.AddNotificationAsync(new Notification
            {
                UserId = order.CustomerId,
                Title = "Atualização do pedido",
                Message = $"O status do seu pedido #{order.Id.ToString()[..8]} mudou para {parsedStatus}.",
                IsRead = false
            });
        }
        await _orderRepository.SaveChangesAsync();

        return MapOrder(order);
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
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.DiscountAmount,
        CouponCode = o.CouponCode,
        DeliveryAddress = o.DeliveryAddress,
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
