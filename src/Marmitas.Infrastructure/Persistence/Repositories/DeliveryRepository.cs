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
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public Task<Courier?> GetCourierByIdAsync(Guid id)
    {
        return _context.Couriers.FirstOrDefaultAsync(c => c.Id == id || c.UserId == id);
    }

    public Task<Courier?> GetCourierByUserIdAsync(Guid userId)
    {
        return _context.Couriers.FirstOrDefaultAsync(c => c.UserId == userId || c.Id == userId);
    }

    public async Task AddCourierAsync(Courier courier)
    {
        await _context.Couriers.AddAsync(courier);
    }

    public async Task<List<CourierDocument>> GetDocumentsByCourierAsync(Guid courierOrUserId)
    {
        var ids = await ResolveCourierIdsAsync(courierOrUserId);
        return await _context.CourierDocuments
            .Where(d => ids.Contains(d.CourierId))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task AddDocumentAsync(CourierDocument document)
    {
        await _context.CourierDocuments.AddAsync(document);
    }

    public async Task<List<CourierVehicle>> GetVehiclesByCourierAsync(Guid courierOrUserId)
    {
        var ids = await ResolveCourierIdsAsync(courierOrUserId);
        return await _context.CourierVehicles
            .Where(v => ids.Contains(v.CourierId))
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

    public async Task<List<Delivery>> GetDeliveriesByCourierAsync(Guid courierOrUserId)
    {
        var ids = await ResolveCourierIdsAsync(courierOrUserId);
        return await _context.Deliveries
            .Where(d => d.CourierId.HasValue && ids.Contains(d.CourierId.Value))
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

    public async Task AddDriverLocationAsync(DriverLocation location)
    {
        await _context.DriverLocations.AddAsync(location);
    }

    public async Task<List<DriverLocation>> GetDriverLocationsAsync(Guid courierOrUserId, int limit = 20)
    {
        var ids = await ResolveCourierIdsAsync(courierOrUserId);
        return await _context.DriverLocations
            .AsNoTracking()
            .Where(l => ids.Contains(l.CourierId))
            .OrderByDescending(l => l.RecordedAt)
            .Take(limit)
            .ToListAsync();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    private async Task<List<Guid>> ResolveCourierIdsAsync(Guid courierOrUserId)
    {
        var list = new List<Guid> { courierOrUserId };
        var courier = await _context.Couriers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courierOrUserId || c.UserId == courierOrUserId);

        if (courier != null)
        {
            if (!list.Contains(courier.Id)) list.Add(courier.Id);
            if (courier.UserId.HasValue && !list.Contains(courier.UserId.Value)) list.Add(courier.UserId.Value);
        }

        return list;
    }
}
