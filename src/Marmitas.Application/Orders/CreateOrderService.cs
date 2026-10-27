using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders.Dtos;
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
            var orderItem = new OrderItem(product.Id, product.Name, product.Price, itemRequest.Quantity);
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

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.Restaurant != null ? o.Restaurant.Name : string.Empty,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(MapItem).ToList()
    };

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
