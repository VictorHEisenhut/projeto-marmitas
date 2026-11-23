using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/entregadores")]
[ApiController]
public class CouriersController : ControllerBase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public CouriersController(
        IDeliveryRepository deliveries,
        IRealtimeNotifier realtimeNotifier)
    {
        _deliveries = deliveries;
        _realtimeNotifier = realtimeNotifier;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCourierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do entregador é obrigatório.");

        var userId = GetUserIdOrNull();
        Courier? courier = null;

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            courier = await _deliveries.GetCourierByUserIdAsync(userId.Value);
        }

        if (courier == null)
        {
            courier = new Courier
            {
                UserId = userId,
                Name = request.Name.Trim(),
                Document = request.Document ?? request.Cpf ?? string.Empty,
                Phone = request.Phone ?? string.Empty,
                Status = "Approved",
                IsAvailable = true,
                BillingStreet = request.BillingStreet?.Trim() ?? string.Empty,
                BillingNumber = request.BillingNumber?.Trim() ?? string.Empty,
                BillingComplement = request.BillingComplement?.Trim(),
                BillingNeighborhood = request.BillingNeighborhood?.Trim() ?? string.Empty,
                BillingCity = request.BillingCity?.Trim() ?? string.Empty,
                BillingState = request.BillingState?.Trim().ToUpperInvariant() ?? string.Empty,
                BillingZipCode = request.BillingZipCode?.Trim() ?? string.Empty
            };
            await _deliveries.AddCourierAsync(courier);
        }
        else
        {
            courier.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Document) || !string.IsNullOrWhiteSpace(request.Cpf))
                courier.Document = (request.Document ?? request.Cpf ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(request.Phone))
                courier.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.BillingStreet))
                courier.BillingStreet = request.BillingStreet.Trim();
            if (!string.IsNullOrWhiteSpace(request.BillingNumber))
                courier.BillingNumber = request.BillingNumber.Trim();
            if (request.BillingComplement != null)
                courier.BillingComplement = request.BillingComplement.Trim();
            if (!string.IsNullOrWhiteSpace(request.BillingNeighborhood))
                courier.BillingNeighborhood = request.BillingNeighborhood.Trim();
            if (!string.IsNullOrWhiteSpace(request.BillingCity))
                courier.BillingCity = request.BillingCity.Trim();
            if (!string.IsNullOrWhiteSpace(request.BillingState))
                courier.BillingState = request.BillingState.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(request.BillingZipCode))
                courier.BillingZipCode = request.BillingZipCode.Trim();
        }

        var targetCourierId = courier.Id;

        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            await UpsertDocumentInternalAsync(targetCourierId, "CPF", request.Cpf.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.Cnh))
        {
            await UpsertDocumentInternalAsync(targetCourierId, "CNH", request.Cnh.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.VehicleModel) && !string.IsNullOrWhiteSpace(request.VehiclePlate))
        {
            await _deliveries.AddVehicleAsync(new CourierVehicle
            {
                CourierId = targetCourierId,
                Type = string.IsNullOrWhiteSpace(request.VehicleType) ? "Moto" : request.VehicleType.Trim(),
                Model = request.VehicleModel.Trim(),
                LicensePlate = request.VehiclePlate.Trim().ToUpperInvariant()
            });
        }

        await _deliveries.SaveChangesAsync();

        return StatusCode(201, courier);
    }

    [HttpGet]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _deliveries.GetAllCouriersAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var courier = await _deliveries.GetCourierByIdAsync(id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        return Ok(courier);
    }

    [HttpPatch("{id}/aprovar")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var courier = await _deliveries.GetCourierByIdAsync(id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        courier.Status = "Approved";
        await _deliveries.SaveChangesAsync();
        return Ok(courier);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] UpdateCourierStatusRequest request)
    {
        var courier = await _deliveries.GetCourierByIdAsync(id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        courier.Status = request.Status;
        await _deliveries.SaveChangesAsync();
        return Ok(courier);
    }

    [HttpGet("me/documentos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyDocuments()
    {
        var list = await _deliveries.GetDocumentsByCourierAsync(GetUserId());
        return Ok(list);
    }

    [HttpPost("me/documentos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UploadDocument([FromBody] SaveCourierDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Number))
            throw new Exception("Tipo e número do documento são obrigatórios.");

        var courier = await EnsureCourierEntityAsync();
        var doc = await UpsertDocumentInternalAsync(courier.Id, request.Type.Trim().ToUpperInvariant(), request.Number.Trim());
        await _deliveries.SaveChangesAsync();
        return StatusCode(201, doc);
    }

    private async Task<CourierDocument> UpsertDocumentInternalAsync(Guid courierId, string type, string number)
    {
        var existing = await _deliveries.GetDocumentsByCourierAsync(courierId);
        var match = existing.FirstOrDefault(d => string.Equals(d.Type, type, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.Number = number;
            match.Status = "Verified";
            return match;
        }

        var doc = new CourierDocument
        {
            CourierId = courierId,
            Type = type,
            Number = number,
            Status = "Verified"
        };
        await _deliveries.AddDocumentAsync(doc);
        return doc;
    }

    [HttpGet("me/veiculos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyVehicles()
    {
        var list = await _deliveries.GetVehiclesByCourierAsync(GetUserId());
        return Ok(list);
    }

    [HttpPost("me/veiculos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> AddVehicle([FromBody] SaveCourierVehicleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.LicensePlate))
            throw new Exception("Modelo e placa do veículo são obrigatórios.");

        var courier = await EnsureCourierEntityAsync();
        var vehicle = new CourierVehicle
        {
            CourierId = courier.Id,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Moto" : request.Type.Trim(),
            Model = request.Model.Trim(),
            LicensePlate = request.LicensePlate.Trim().ToUpperInvariant()
        };

        await _deliveries.AddVehicleAsync(vehicle);
        await _deliveries.SaveChangesAsync();
        return StatusCode(201, vehicle);
    }

    [HttpPut("me/veiculos/{id}")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] SaveCourierVehicleRequest request)
    {
        var vehicle = await _deliveries.GetVehicleByIdAsync(id);
        if (vehicle == null)
            throw new Exception("Veículo não encontrado.");

        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
        {
            var myVehicles = await _deliveries.GetVehiclesByCourierAsync(GetUserId());
            if (!myVehicles.Any(v => v.Id == id))
                return Forbid();
        }

        vehicle.Type = request.Type.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.LicensePlate = request.LicensePlate.Trim().ToUpperInvariant();

        await _deliveries.SaveChangesAsync();
        return Ok(vehicle);
    }

    [HttpGet("me/endereco-cobranca")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyBillingAddress()
    {
        var userId = GetUserId();
        var courier = await _deliveries.GetCourierByUserIdAsync(userId);

        var isComplete = courier != null &&
            !string.IsNullOrWhiteSpace(courier.BillingStreet) &&
            !string.IsNullOrWhiteSpace(courier.BillingNumber) &&
            !string.IsNullOrWhiteSpace(courier.BillingNeighborhood) &&
            !string.IsNullOrWhiteSpace(courier.BillingCity) &&
            !string.IsNullOrWhiteSpace(courier.BillingState) &&
            !string.IsNullOrWhiteSpace(courier.BillingZipCode);

        return Ok(new CourierBillingAddressDto
        {
            Street = courier?.BillingStreet ?? string.Empty,
            Number = courier?.BillingNumber ?? string.Empty,
            Complement = courier?.BillingComplement,
            Neighborhood = courier?.BillingNeighborhood ?? string.Empty,
            City = courier?.BillingCity ?? string.Empty,
            State = courier?.BillingState ?? string.Empty,
            ZipCode = courier?.BillingZipCode ?? string.Empty,
            IsComplete = isComplete
        });
    }

    [HttpPut("me/endereco-cobranca")]
    [HttpPost("me/endereco-cobranca")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> SaveMyBillingAddress([FromBody] SaveCourierBillingAddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Street) ||
            string.IsNullOrWhiteSpace(request.Number) ||
            string.IsNullOrWhiteSpace(request.Neighborhood) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) ||
            string.IsNullOrWhiteSpace(request.ZipCode))
        {
            throw new Exception("Preencha todos os campos obrigatórios do endereço de cobrança (CEP, Rua, Número, Bairro, Cidade e UF).");
        }

        var courier = await EnsureCourierEntityAsync();
        courier.BillingStreet = request.Street.Trim();
        courier.BillingNumber = request.Number.Trim();
        courier.BillingComplement = request.Complement?.Trim();
        courier.BillingNeighborhood = request.Neighborhood.Trim();
        courier.BillingCity = request.City.Trim();
        courier.BillingState = request.State.Trim().ToUpperInvariant();
        courier.BillingZipCode = request.ZipCode.Trim();

        await _deliveries.SaveChangesAsync();

        return Ok(new CourierBillingAddressDto
        {
            Street = courier.BillingStreet,
            Number = courier.BillingNumber,
            Complement = courier.BillingComplement,
            Neighborhood = courier.BillingNeighborhood,
            City = courier.BillingCity,
            State = courier.BillingState,
            ZipCode = courier.BillingZipCode,
            IsComplete = true
        });
    }

    [HttpPatch("me/localizacao")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var courier = await EnsureCourierEntityAsync();
        courier.Latitude = request.Latitude;
        courier.Longitude = request.Longitude;

        await _deliveries.AddDriverLocationAsync(new DriverLocation
        {
            CourierId = courier.Id,
            DeliveryId = request.DeliveryId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = DateTime.UtcNow
        });

        await _deliveries.SaveChangesAsync();

        if (request.OrderId.HasValue)
        {
            await _realtimeNotifier.NotifyDriverLocationAsync(
                request.OrderId.Value,
                courier.Id,
                request.Latitude,
                request.Longitude);
        }

        return Ok(new
        {
            courierId = courier.Id,
            latitude = request.Latitude,
            longitude = request.Longitude,
            updatedAt = DateTime.UtcNow
        });
    }

    [HttpGet("me/entregas")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyDeliveries()
    {
        var list = await _deliveries.GetDeliveriesByCourierAsync(GetUserId());
        return Ok(list);
    }

    [HttpGet("{id}/historico")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetDeliveryHistory(Guid id)
    {
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
        {
            var courier = await _deliveries.GetCourierByUserIdAsync(GetUserId());
            if (courier == null || (courier.Id != id && courier.UserId != id && GetUserId() != id))
                return Forbid();
        }

        var list = await _deliveries.GetDeliveriesByCourierAsync(id);
        return Ok(list);
    }

    [HttpGet("me/ganhos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyEarnings()
    {
        var userId = GetUserId();
        var deliveries = await _deliveries.GetDeliveriesByCourierAsync(userId);
        var completed = deliveries.Where(d => d.Status == "Delivered").ToList();
        var totalEarnings = completed.Sum(d => d.DeliveryFee > 0 ? d.DeliveryFee : 8.50m);

        return Ok(new
        {
            courierId = userId,
            completedDeliveries = completed.Count,
            totalEarnings
        });
    }

    private async Task<Courier> EnsureCourierEntityAsync()
    {
        var userId = GetUserId();
        var courier = await _deliveries.GetCourierByUserIdAsync(userId);
        if (courier == null)
        {
            courier = new Courier
            {
                UserId = userId,
                Name = User.Identity?.Name ?? "Entregador",
                Status = "Approved",
                IsAvailable = true
            };
            await _deliveries.AddCourierAsync(courier);
        }

        return courier;
    }

    private Guid GetUserId()
    {
        return GetUserIdOrNull() ?? Guid.Empty;
    }

    private Guid? GetUserIdOrNull()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
