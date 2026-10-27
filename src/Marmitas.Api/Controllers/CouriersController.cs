using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/entregadores")]
[ApiController]
public class CouriersController : ControllerBase
{
    public static readonly List<CourierDto> Couriers = new();
    private static readonly List<CourierDocumentDto> Documents = new();
    private static readonly List<CourierVehicleDto> Vehicles = new();

    [HttpPost("registro")]
    [AllowAnonymous]
    public IActionResult Register([FromBody] RegisterCourierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do entregador é obrigatório.");

        var courier = new CourierDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Document = request.Document ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            Status = "Approved",
            IsAvailable = true
        };

        Couriers.Add(courier);
        return StatusCode(201, courier);
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetAll()
    {
        return Ok(Couriers);
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetById(Guid id)
    {
        var courier = Couriers.FirstOrDefault(c => c.Id == id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        return Ok(courier);
    }

    [HttpPatch("{id}/aprovar")]
    [Authorize]
    public IActionResult Approve(Guid id)
    {
        var courier = Couriers.FirstOrDefault(c => c.Id == id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        courier.Status = "Approved";
        return Ok(courier);
    }

    [HttpPatch("{id}/status")]
    [Authorize]
    public IActionResult ChangeStatus(Guid id, [FromBody] UpdateCourierStatusRequest request)
    {
        var courier = Couriers.FirstOrDefault(c => c.Id == id);
        if (courier == null)
            throw new Exception("Entregador não encontrado.");

        courier.Status = request.Status;
        return Ok(courier);
    }

    [HttpGet("me/documentos")]
    [Authorize]
    public IActionResult GetMyDocuments()
    {
        var userId = GetUserId();
        return Ok(Documents.Where(d => d.CourierId == userId));
    }

    [HttpPost("me/documentos")]
    [Authorize]
    public IActionResult UploadDocument([FromBody] SaveCourierDocumentRequest request)
    {
        var doc = new CourierDocumentDto
        {
            Id = Guid.NewGuid(),
            CourierId = GetUserId(),
            Type = request.Type,
            Number = request.Number,
            Status = "Verified"
        };

        Documents.Add(doc);
        return StatusCode(201, doc);
    }

    [HttpGet("me/veiculos")]
    [Authorize]
    public IActionResult GetMyVehicles()
    {
        var userId = GetUserId();
        return Ok(Vehicles.Where(v => v.CourierId == userId));
    }

    [HttpPost("me/veiculos")]
    [Authorize]
    public IActionResult AddVehicle([FromBody] SaveCourierVehicleRequest request)
    {
        var vehicle = new CourierVehicleDto
        {
            Id = Guid.NewGuid(),
            CourierId = GetUserId(),
            Type = request.Type,
            Model = request.Model,
            LicensePlate = request.LicensePlate
        };

        Vehicles.Add(vehicle);
        return StatusCode(201, vehicle);
    }

    [HttpPut("me/veiculos/{id}")]
    [Authorize]
    public IActionResult UpdateVehicle(Guid id, [FromBody] SaveCourierVehicleRequest request)
    {
        var vehicle = Vehicles.FirstOrDefault(v => v.Id == id);
        if (vehicle == null)
            throw new Exception("Veículo não encontrado.");

        vehicle.Type = request.Type;
        vehicle.Model = request.Model;
        vehicle.LicensePlate = request.LicensePlate;
        return Ok(vehicle);
    }

    [HttpPatch("me/localizacao")]
    [Authorize]
    public IActionResult UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        return Ok(new
        {
            courierId = GetUserId(),
            latitude = request.Latitude,
            longitude = request.Longitude,
            updatedAt = DateTime.UtcNow
        });
    }

    [HttpGet("me/entregas")]
    [Authorize]
    public IActionResult GetMyDeliveries()
    {
        return Ok(DeliveriesController.Deliveries.Where(d => d.CourierId == GetUserId()));
    }

    [HttpGet("{id}/historico")]
    [Authorize]
    public IActionResult GetDeliveryHistory(Guid id)
    {
        return Ok(DeliveriesController.Deliveries.Where(d => d.CourierId == id));
    }

    [HttpGet("me/ganhos")]
    [Authorize]
    public IActionResult GetMyEarnings()
    {
        var userId = GetUserId();
        var count = DeliveriesController.Deliveries.Count(d => d.CourierId == userId && d.Status == "Delivered");
        return Ok(new
        {
            courierId = userId,
            completedDeliveries = count,
            totalEarnings = count * 8.50m
        });
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public class CourierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
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

public class CourierDocumentDto
{
    public Guid Id { get; set; }
    public Guid CourierId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SaveCourierDocumentRequest
{
    public string Type { get; set; } = "CNH";
    public string Number { get; set; } = string.Empty;
}

public class CourierVehicleDto
{
    public Guid Id { get; set; }
    public Guid CourierId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
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
