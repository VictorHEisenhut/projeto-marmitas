using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/restaurantes")]
[ApiController]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;

    public RestaurantsController(
        IRestaurantQueryService restaurantQueryService,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository)
    {
        _restaurantQueryService = restaurantQueryService;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool includeAll = false)
    {
        if (includeAll && (User.IsInRole("admin") || User.IsInRole("super_admin")))
        {
            var all = await _restaurantQueryService.GetAllAsync();
            return Ok(all);
        }

        var restaurants = await _restaurantQueryService.GetAllPublicAsync();
        return Ok(restaurants);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await _restaurantQueryService.GetByIdAsync(id);
        return Ok(restaurant);
    }

    [HttpPost]
    [Authorize(Roles = "restaurant_owner,admin,super_admin")]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _restaurantQueryService.CreateAsync(userId.Value, request);
        return StatusCode(201, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRestaurantRequest request)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var result = await _restaurantQueryService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] UpdateRestaurantStatusRequest request)
    {
        var result = await _restaurantQueryService.ChangeStatusAsync(id, request.Status);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _restaurantQueryService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/cardapio")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(Guid id, [FromQuery] bool includeInactive = false)
    {
        var products = await _restaurantQueryService.GetRestaurantProductsAsync(id, includeInactive);
        return Ok(products);
    }

    [HttpGet("{id}/horarios")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHours(Guid id)
    {
        var hours = await _restaurantRepository.GetRestaurantHoursAsync(id);
        if (hours.Count == 0)
        {
            var defaults = Enumerable.Range(0, 7).Select(day => new RestaurantHourDto
            {
                DayOfWeek = day,
                OpenTime = "10:30",
                CloseTime = "15:30",
                IsOpen = day != 0
            }).ToList();
            return Ok(defaults);
        }

        return Ok(hours.Select(h => new RestaurantHourDto
        {
            DayOfWeek = h.DayOfWeek,
            OpenTime = h.OpenTime,
            CloseTime = h.CloseTime,
            IsOpen = h.IsOpen
        }));
    }

    [HttpPut("{id}/horarios")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> UpdateHours(Guid id, [FromBody] List<RestaurantHourDto> request)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var entities = request.Select(h => new RestaurantHour
        {
            RestaurantId = id,
            DayOfWeek = h.DayOfWeek,
            OpenTime = string.IsNullOrWhiteSpace(h.OpenTime) ? "10:30" : h.OpenTime.Trim(),
            CloseTime = string.IsNullOrWhiteSpace(h.CloseTime) ? "15:30" : h.CloseTime.Trim(),
            IsOpen = h.IsOpen
        }).ToList();

        await _restaurantRepository.SetRestaurantHoursAsync(id, entities);
        await _restaurantRepository.SaveChangesAsync();
        return Ok(request);
    }

    [HttpGet("{id}/zonas-entrega")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDeliveryZones(Guid id)
    {
        var zones = await _restaurantRepository.GetDeliveryZonesAsync(id);
        return Ok(zones);
    }

    [HttpPost("{id}/zonas-entrega")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> AddDeliveryZone(Guid id, [FromBody] SaveDeliveryZoneRequest request)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.Neighborhood))
            throw new Exception("O bairro ou região da zona de entrega é obrigatório.");

        var zone = new DeliveryZone
        {
            RestaurantId = id,
            Neighborhood = request.Neighborhood.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? "Blumenau" : request.City.Trim(),
            RadiusKm = Math.Max(1.0, request.RadiusKm),
            DeliveryFee = Math.Max(0m, request.DeliveryFee),
            EstimatedMinutes = Math.Max(10, request.EstimatedMinutes),
            IsActive = true
        };

        await _restaurantRepository.AddDeliveryZoneAsync(zone);
        await _restaurantRepository.SaveChangesAsync();
        return StatusCode(201, zone);
    }

    [HttpDelete("{id}/zonas-entrega/{zoneId}")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> DeleteDeliveryZone(Guid id, Guid zoneId)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var zone = await _restaurantRepository.GetDeliveryZoneByIdAsync(zoneId);
        if (zone == null || zone.RestaurantId != id)
            throw new Exception("Zona de entrega não encontrada.");

        _restaurantRepository.RemoveDeliveryZone(zone);
        await _restaurantRepository.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/equipe")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetTeam(Guid id)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var team = await _restaurantRepository.GetRestaurantUsersAsync(id);
        var result = new List<object>();

        foreach (var member in team)
        {
            var user = await _userRepository.GetByIdAsync(member.UserId);
            result.Add(new
            {
                id = member.Id,
                restaurantId = member.RestaurantId,
                userId = member.UserId,
                userName = user?.Name ?? "Colaborador",
                userEmail = user?.Email ?? string.Empty,
                role = member.Role,
                isActive = member.IsActive
            });
        }

        return Ok(result);
    }

    [HttpPost("{id}/equipe")]
    [Authorize(Roles = "restaurant_owner,admin,super_admin")]
    public async Task<IActionResult> AddTeamMember(Guid id, [FromBody] AddRestaurantStaffRequest request)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new Exception("Informe o e-mail do usuário para vincular ao restaurante.");

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLower());
        if (user == null)
            throw new Exception("Nenhum usuário encontrado com o e-mail informado.");

        var staffRole = await _userRepository.GetRoleByNameAsync("restaurant_staff");
        if (staffRole != null && !user.GetRoleNames().Contains("restaurant_staff"))
        {
            user.AddRole(staffRole);
        }

        var member = new RestaurantUser
        {
            RestaurantId = id,
            UserId = user.Id,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "restaurant_staff" : request.Role.Trim(),
            IsActive = true
        };

        await _restaurantRepository.AddRestaurantUserAsync(member);
        await _restaurantRepository.SaveChangesAsync();

        return StatusCode(201, new
        {
            id = member.Id,
            restaurantId = member.RestaurantId,
            userId = user.Id,
            userName = user.Name,
            userEmail = user.Email,
            role = member.Role,
            isActive = member.IsActive
        });
    }

    [HttpDelete("{id}/equipe/{memberId}")]
    [Authorize(Roles = "restaurant_owner,admin,super_admin")]
    public async Task<IActionResult> RemoveTeamMember(Guid id, Guid memberId)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var team = await _restaurantRepository.GetRestaurantUsersAsync(id);
        var target = team.FirstOrDefault(m => m.Id == memberId || m.UserId == memberId);
        if (target == null)
            throw new Exception("Vínculo de colaborador não encontrado.");

        _restaurantRepository.RemoveRestaurantUser(target);
        await _restaurantRepository.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> CanManageRestaurantAsync(Guid restaurantId)
    {
        if (User.IsInRole("admin") || User.IsInRole("super_admin"))
            return true;

        var userId = GetCurrentUserId();
        if (userId == null) return false;

        return await _restaurantQueryService.IsUserAuthorizedAsync(restaurantId, userId.Value, false);
    }

    private Guid? GetCurrentUserId()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}
