using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId);
    Task<List<Order>> GetAllAsync();
    Task<int> SaveChangesAsync();
}
