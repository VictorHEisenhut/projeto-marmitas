using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ICustomerProfileRepository _profiles;

    public NotificationsController(ICustomerProfileRepository profiles)
    {
        _profiles = profiles;
    }

    [HttpGet("api/notificacoes")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetUserId();
        var list = await _profiles.GetNotificationsByUserAsync(userId);
        return Ok(list);
    }

    [HttpPatch("api/notificacoes/{id}/lida")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notif = await _profiles.GetNotificationByIdAsync(id);
        if (notif == null)
            throw new Exception("Notificação não encontrada.");

        if (notif.UserId != GetUserId() && !User.IsInRole("admin") && !User.IsInRole("super_admin"))
            return Forbid();

        notif.IsRead = true;
        await _profiles.SaveChangesAsync();
        return Ok(notif);
    }

    [HttpPatch("api/notificacoes/marcar-todas-lidas")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        var list = await _profiles.GetNotificationsByUserAsync(userId);
        foreach (var n in list)
            n.IsRead = true;

        await _profiles.SaveChangesAsync();
        return Ok(new { message = "Todas as notificações foram marcadas como lidas." });
    }

    [HttpDelete("api/notificacoes/{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var notif = await _profiles.GetNotificationByIdAsync(id);
        if (notif == null)
            throw new Exception("Notificação não encontrada.");

        if (notif.UserId != GetUserId() && !User.IsInRole("admin") && !User.IsInRole("super_admin"))
            return Forbid();

        _profiles.RemoveNotification(notif);
        await _profiles.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("api/usuarios/me/preferencias-notificacao")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var prefs = await _profiles.GetPreferencesByUserAsync(userId);
        if (prefs == null)
        {
            prefs = new NotificationPreference
            {
                UserId = userId,
                EmailEnabled = true,
                PushEnabled = true,
                SmsEnabled = false
            };
            await _profiles.AddPreferencesAsync(prefs);
            await _profiles.SaveChangesAsync();
        }

        return Ok(new NotificationPreferencesDto
        {
            EmailEnabled = prefs.EmailEnabled,
            PushEnabled = prefs.PushEnabled,
            SmsEnabled = prefs.SmsEnabled
        });
    }

    [HttpPut("api/usuarios/me/preferencias-notificacao")]
    public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferencesDto request)
    {
        var userId = GetUserId();
        var prefs = await _profiles.GetPreferencesByUserAsync(userId);
        if (prefs == null)
        {
            prefs = new NotificationPreference { UserId = userId };
            await _profiles.AddPreferencesAsync(prefs);
        }

        prefs.EmailEnabled = request.EmailEnabled;
        prefs.PushEnabled = request.PushEnabled;
        prefs.SmsEnabled = request.SmsEnabled;

        await _profiles.SaveChangesAsync();
        return Ok(request);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
