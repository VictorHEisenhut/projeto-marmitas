using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IDeliveryRepository
{
    Task<List<Courier>> GetAllCouriersAsync();
    Task<Courier?> GetCourierByIdAsync(Guid id);
    Task<Courier?> GetCourierByUserIdAsync(Guid userId);
    Task AddCourierAsync(Courier courier);

    Task<List<CourierDocument>> GetDocumentsByCourierAsync(Guid courierId);
    Task AddDocumentAsync(CourierDocument document);

    Task<List<CourierVehicle>> GetVehiclesByCourierAsync(Guid courierId);
    Task<CourierVehicle?> GetVehicleByIdAsync(Guid id);
    Task AddVehicleAsync(CourierVehicle vehicle);

    Task<List<Delivery>> GetAllDeliveriesAsync();
    Task<List<Delivery>> GetDeliveriesByCourierAsync(Guid courierId);
    Task<Delivery?> GetDeliveryByIdOrOrderAsync(Guid id);
    Task AddDeliveryAsync(Delivery delivery);

    Task SaveChangesAsync();
}

