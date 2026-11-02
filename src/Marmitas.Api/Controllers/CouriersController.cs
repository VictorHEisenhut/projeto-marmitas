using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/entregadores")]
[ApiController]
public class CouriersController : ControllerBase
{
    private readonly IDeliveryRepository _deliveries;

    public CouriersController(IDeliveryRepository deliveries)
    {
        _deliveries = deliveries;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCourierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do entregador é obrigatório.");

        var userId = GetUserIdOrNull();
        var courier = new Courier
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Document = request.Document ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            Status = "Pending",
            IsAvailable = true
        };

        await _deliveries.AddCourierAsync(courier);
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
        var doc = new CourierDocument
        {
            CourierId = GetUserId(),
            Type = request.Type,
            Number = request.Number,
            Status = "Verified"
        };

        await _deliveries.AddDocumentAsync(doc);
        await _deliveries.SaveChangesAsync();
        return StatusCode(201, doc);
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
        var vehicle = new CourierVehicle
        {
            CourierId = GetUserId(),
            Type = request.Type,
            Model = request.Model,
            LicensePlate = request.LicensePlate
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

        vehicle.Type = request.Type;
        vehicle.Model = request.Model;
        vehicle.LicensePlate = request.LicensePlate;

        await _deliveries.SaveChangesAsync();
        return Ok(vehicle);
    }

    [HttpPatch("me/localizacao")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var userId = GetUserId();
        var courier = await _deliveries.GetCourierByUserIdAsync(userId);
        if (courier != null)
        {
            courier.Latitude = request.Latitude;
            courier.Longitude = request.Longitude;
            await _deliveries.SaveChangesAsync();
        }

        return Ok(new
        {
            courierId = userId,
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
        var list = await _deliveries.GetDeliveriesByCourierAsync(id);
        return Ok(list);
    }

    [HttpGet("me/ganhos")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetMyEarnings()
    {
        var userId = GetUserId();
        var deliveries = await _deliveries.GetDeliveriesByCourierAsync(userId);
        var count = deliveries.Count(d => d.Status == "Delivered");

        return Ok(new
        {
            courierId = userId,
            completedDeliveries = count,
            totalEarnings = count * 8.50m
        });
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

public class RegisterCourierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Phone { get; set; }
}

public class UpdateCourierStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SaveCourierDocumentRequest
{
    public string Type { get; set; } = "CNH";
    public string Number { get; set; } = string.Empty;
}

public class SaveCourierVehicleRequest
{
    public string Type { get; set; } = "Moto";
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}

public class UpdateLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
