using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users;
using Marmitas.Application.Users.Dtos;
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

    public UsersController(IUserRepository users)
    {
        _users = users;
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

        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Blocked;
        await _users.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.GetAllAsync();
        return Ok(users.Select(UserMapper.ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) throw new Exception("Usuário não encontrado.");

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("{id}")]
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
