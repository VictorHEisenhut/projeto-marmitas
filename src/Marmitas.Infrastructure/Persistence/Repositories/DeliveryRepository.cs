using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly AppDbContext _context;

    public DeliveryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Courier>> GetAllCouriersAsync()
    {
        return _context.Couriers
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public Task<Courier?> GetCourierByIdAsync(Guid id)
    {
        return _context.Couriers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<Courier?> GetCourierByUserIdAsync(Guid userId)
    {
        return _context.Couriers.FirstOrDefaultAsync(c => c.UserId == userId || c.Id == userId);
    }

    public async Task AddCourierAsync(Courier courier)
    {
        await _context.Couriers.AddAsync(courier);
    }

    public Task<List<CourierDocument>> GetDocumentsByCourierAsync(Guid courierId)
    {
        return _context.CourierDocuments
            .Where(d => d.CourierId == courierId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task AddDocumentAsync(CourierDocument document)
    {
        await _context.CourierDocuments.AddAsync(document);
    }

    public Task<List<CourierVehicle>> GetVehiclesByCourierAsync(Guid courierId)
    {
        return _context.CourierVehicles
            .Where(v => v.CourierId == courierId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public Task<CourierVehicle?> GetVehicleByIdAsync(Guid id)
    {
        return _context.CourierVehicles.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task AddVehicleAsync(CourierVehicle vehicle)
    {
        await _context.CourierVehicles.AddAsync(vehicle);
    }

    public Task<List<Delivery>> GetAllDeliveriesAsync()
    {
        return _context.Deliveries
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
    }

    public Task<List<Delivery>> GetDeliveriesByCourierAsync(Guid courierId)
    {
        return _context.Deliveries
            .Where(d => d.CourierId == courierId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
    }

    public Task<Delivery?> GetDeliveryByIdOrOrderAsync(Guid id)
    {
        return _context.Deliveries.FirstOrDefaultAsync(d => d.Id == id || d.OrderId == id);
    }

    public async Task AddDeliveryAsync(Delivery delivery)
    {
        await _context.Deliveries.AddAsync(delivery);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}

