using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/enderecos")]
[ApiController]
[Authorize]
public class AddressesController : ControllerBase
{
    private static readonly List<AddressDto> Addresses = new();

    [HttpGet]
    public IActionResult GetMyAddresses()
    {
        var userId = GetUserId();
        var list = Addresses.Where(a => a.UserId == userId).ToList();
        return Ok(list);
    }

    [HttpPost]
    public IActionResult CreateAddress([FromBody] SaveAddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.City))
            throw new Exception("Rua e cidade são obrigatórias.");

        var userId = GetUserId();
        if (request.IsMain)
        {
            foreach (var addr in Addresses.Where(a => a.UserId == userId))
                addr.IsMain = false;
        }

        var address = new AddressDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Street = request.Street.Trim(),
            Number = request.Number.Trim(),
            Complement = request.Complement,
            Neighborhood = request.Neighborhood.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            ZipCode = request.ZipCode.Trim(),
            IsMain = request.IsMain || !Addresses.Any(a => a.UserId == userId)
        };

        Addresses.Add(address);
        return StatusCode(201, address);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateAddress(Guid id, [FromBody] SaveAddressRequest request)
    {
        var userId = GetUserId();
        var address = Addresses.FirstOrDefault(a => a.Id == id && a.UserId == userId);
        if (address == null)
            throw new Exception("Endereço não encontrado.");

        address.Street = request.Street.Trim();
        address.Number = request.Number.Trim();
        address.Complement = request.Complement;
        address.Neighborhood = request.Neighborhood.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.ZipCode = request.ZipCode.Trim();

        return Ok(address);
    }

    [HttpPatch("{id}/principal")]
    public IActionResult SetMainAddress(Guid id)
    {
        var userId = GetUserId();
        var address = Addresses.FirstOrDefault(a => a.Id == id && a.UserId == userId);
        if (address == null)
            throw new Exception("Endereço não encontrado.");

        foreach (var addr in Addresses.Where(a => a.UserId == userId))
            addr.IsMain = false;

        address.IsMain = true;
        return Ok(address);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteAddress(Guid id)
    {
        var userId = GetUserId();
        var address = Addresses.FirstOrDefault(a => a.Id == id && a.UserId == userId);
        if (address == null)
            throw new Exception("Endereço não encontrado.");

        Addresses.Remove(address);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public class AddressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public class SaveAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}
