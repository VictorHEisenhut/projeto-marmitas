using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/enderecos")]
[ApiController]
[Authorize(Roles = "customer,admin,super_admin")]
public class AddressesController : ControllerBase
{
    private readonly ICustomerProfileRepository _profiles;

    public AddressesController(ICustomerProfileRepository profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = GetUserId();
        var list = await _profiles.GetAddressesByUserAsync(userId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] SaveAddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.City))
            throw new Exception("Rua e cidade são obrigatórias.");

        var userId = GetUserId();
        var existing = await _profiles.GetAddressesByUserAsync(userId);

        if (request.IsMain)
        {
            foreach (var addr in existing)
                addr.IsMain = false;
        }

        var street = request.Street.Trim();
        var neighborhood = request.Neighborhood.Trim();
        var city = request.City.Trim();
        var state = request.State.Trim().ToUpperInvariant();
        var zipCode = request.ZipCode.Trim();
        var resolvedPoint = GeoCalculator.ResolveAddressCoordinates(
            request.Latitude,
            request.Longitude,
            street,
            neighborhood,
            city,
            state,
            zipCode);

        var address = new Address
        {
            UserId = userId,
            Street = street,
            Number = request.Number.Trim(),
            Complement = request.Complement,
            Neighborhood = neighborhood,
            City = city,
            State = state,
            ZipCode = zipCode,
            Latitude = resolvedPoint.Latitude,
            Longitude = resolvedPoint.Longitude,
            IsMain = request.IsMain || existing.Count == 0
        };

        await _profiles.AddAddressAsync(address);
        await _profiles.SaveChangesAsync();

        return StatusCode(201, address);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] SaveAddressRequest request)
    {
        var userId = GetUserId();
        var address = await _profiles.GetAddressByIdAsync(id, userId);
        if (address == null)
            throw new Exception("Endereço não encontrado.");

        var addressChanged =
            !string.Equals(address.Street, request.Street.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Neighborhood, request.Neighborhood.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.City, request.City.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.State, request.State.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.ZipCode, request.ZipCode.Trim(), StringComparison.OrdinalIgnoreCase);

        address.Street = request.Street.Trim();
        address.Number = request.Number.Trim();
        address.Complement = request.Complement;
        address.Neighborhood = request.Neighborhood.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim().ToUpperInvariant();
        address.ZipCode = request.ZipCode.Trim();

        var resolvedPoint = GeoCalculator.ResolveAddressCoordinates(
            addressChanged ? request.Latitude : (request.Latitude ?? address.Latitude),
            addressChanged ? request.Longitude : (request.Longitude ?? address.Longitude),
            address.Street,
            address.Neighborhood,
            address.City,
            address.State,
            address.ZipCode);
        address.Latitude = resolvedPoint.Latitude;
        address.Longitude = resolvedPoint.Longitude;

        await _profiles.SaveChangesAsync();
        return Ok(address);
    }

    [HttpPatch("{id}/principal")]
    public async Task<IActionResult> SetMainAddress(Guid id)
    {
        var userId = GetUserId();
        var addresses = await _profiles.GetAddressesByUserAsync(userId);
        var target = addresses.FirstOrDefault(a => a.Id == id);
        if (target == null)
            throw new Exception("Endereço não encontrado.");

        foreach (var addr in addresses)
            addr.IsMain = addr.Id == id;

        await _profiles.SaveChangesAsync();
        return Ok(target);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var userId = GetUserId();
        var address = await _profiles.GetAddressByIdAsync(id, userId);
        if (address == null)
            throw new Exception("Endereço não encontrado.");

        _profiles.RemoveAddress(address);
        await _profiles.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
