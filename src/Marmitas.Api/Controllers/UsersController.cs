using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Users;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICustomerProfileRepository _profiles;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ICreateOrderService _orders;

    public UsersController(
        IUserRepository users,
        ICustomerProfileRepository profiles,
        ISubscriptionRepository subscriptions,
        ICreateOrderService orders)
    {
        _users = users;
        _profiles = profiles;
        _subscriptions = subscriptions;
        _orders = orders;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) throw new Exception("Usuário não encontrado.");

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) throw new Exception("Usuário não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name.Trim();
        user.Phone = request.Phone;

        await _users.SaveChangesAsync();
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) throw new Exception("Usuário não encontrado.");

        var addresses = await _profiles.GetAddressesByUserAsync(userId.Value);
        foreach (var addr in addresses)
            _profiles.RemoveAddress(addr);

        var favorites = await _profiles.GetFavoritesByUserAsync(userId.Value);
        foreach (var fav in favorites)
            _profiles.RemoveFavorite(fav);

        var activeSubs = await _subscriptions.GetSubscriptionsByUserAsync(userId.Value);
        foreach (var sub in activeSubs.Where(s => s.Status == "Active" || s.Status == "Paused"))
            sub.Cancel();

        user.Anonymize();
        await _users.RevokeUserRefreshTokensAsync(userId.Value);
        await _users.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("me/exportar-dados")]
    public async Task<IActionResult> ExportMyData()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) throw new Exception("Usuário não encontrado.");

        var addresses = await _profiles.GetAddressesByUserAsync(userId.Value);
        var favorites = await _profiles.GetFavoritesByUserAsync(userId.Value);
        var consents = await _users.GetUserConsentsAsync(userId.Value);
        var subscriptions = await _subscriptions.GetSubscriptionsByUserAsync(userId.Value);
        var orders = await _orders.GetMyOrdersAsync(userId.Value);

        return Ok(new
        {
            exportedAt = DateTime.UtcNow,
            lgpdCompliance = "Relatório de Portabilidade e Direito de Acesso do Titular (Art. 18 LGPD)",
            profile = UserMapper.ToResponse(user),
            consents,
            addresses,
            favorites,
            subscriptions,
            orders
        });
    }

    [HttpGet("me/consentimentos")]
    public async Task<IActionResult> GetMyConsents()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var consents = await _users.GetUserConsentsAsync(userId.Value);
        return Ok(consents);
    }

    [HttpPost("me/consentimentos")]
    public async Task<IActionResult> RecordConsent([FromBody] RecordUserConsentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var consent = new UserConsent
        {
            UserId = userId.Value,
            Purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "TermsAndPrivacy" : request.Purpose.Trim(),
            TermsVersion = string.IsNullOrWhiteSpace(request.TermsVersion) ? "v1.0" : request.TermsVersion.Trim(),
            IsGranted = request.IsGranted,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ConsentedAt = DateTime.UtcNow
        };

        await _users.AddUserConsentAsync(consent);
        await _users.SaveChangesAsync();
        return StatusCode(201, consent);
    }

    [HttpGet]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.GetAllAsync();
        return Ok(users.Select(UserMapper.ToResponse));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name.Trim();
        user.Phone = request.Phone;

        await _users.SaveChangesAsync();
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        if (!Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
            throw new Exception("Status inválido. Use Active ou Blocked.");

        user.Status = parsedStatus;
        await _users.SaveChangesAsync();
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Blocked;
        await _users.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/roles")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        user.UserRoles.Clear();
        foreach (var roleName in request.Roles.Distinct())
        {
            var role = await _users.GetRoleByNameAsync(roleName.Trim().ToLower());
            if (role == null)
                throw new Exception($"Role '{roleName}' não encontrada.");

            user.AddRole(role);
        }

        await _users.SaveChangesAsync();
        return Ok(UserMapper.ToResponse(user));
    }

    private Guid? GetCurrentUserId()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}

public class RecordUserConsentRequest
{
    public string Purpose { get; set; } = "TermsAndPrivacy";
    public string TermsVersion { get; set; } = "v1.0";
    public bool IsGranted { get; set; } = true;
}
