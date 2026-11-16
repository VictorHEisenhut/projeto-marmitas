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

    public CreateOrderService(IRestaurantRepository restaurantRepository, IOrderRepository orderRepository)
    {
        _restaurantRepository = restaurantRepository;
        _orderRepository = orderRepository;
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

        var order = new Order(customerId, request.RestaurantId);

        foreach (var itemRequest in request.Items)
        {
            var product = activeProducts.First(p => p.Id == itemRequest.ProductId);

            // Validação Crucial 1 (Plano Mensal): Somente marmitas com AvailableInPlans == true
            if (request.IsPlanRedemption && !product.AvailableInPlans)
            {
                throw new Exception($"A marmita '{product.Name}' não está disponível para resgate no plano mensal.");
            }

            // Validação de Talheres
            if (itemRequest.WantsCutlery && !product.AllowsCutlery)
            {
                throw new Exception($"O prato '{product.Name}' não permite opção de talheres.");
            }

            var cutleryCost = itemRequest.WantsCutlery && product.AllowsCutlery && product.CutleryHasExtraCost
                ? product.CutleryPrice
                : 0m;

            // Validação e cálculo dos Adicionais Dinâmicos
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

            // Validação Crucial 2 e 3:
            // Modalidade Avulsa: Valor da Marmita + Adicionais + Talher
            // Modalidade Plano Mensal: Base = 0 (coberto pelo plano) + apenas Adicionais pagos e Talher cobrado
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

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
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
