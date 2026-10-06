using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;

namespace Marmitas.Application.Orders;

public interface ICreateOrderService
{
    Task<CreateOrderResponse> ExecuteAsync(Guid customerId, CreateOrderRequest request);
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
}
