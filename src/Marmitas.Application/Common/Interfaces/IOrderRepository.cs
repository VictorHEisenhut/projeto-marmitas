using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<int> SaveChangesAsync();
}

