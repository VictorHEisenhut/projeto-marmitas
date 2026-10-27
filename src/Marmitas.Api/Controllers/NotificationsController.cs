using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private static readonly List<NotificationDto> Notifications = new();
    private static NotificationPreferencesDto Preferences = new()
    {
        EmailEnabled = true,
        PushEnabled = true,
        SmsEnabled = false
    };

    [HttpGet("api/notificacoes")]
    public IActionResult GetMyNotifications()
    {
        var userId = GetUserId();
        if (!Notifications.Any(n => n.UserId == userId))
        {
            Notifications.Add(new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Bem-vindo ao Marmitas!",
                Message = "Sua conta está pronta para realizar pedidos.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Ok(Notifications.Where(n => n.UserId == userId));
    }

    [HttpPatch("api/notificacoes/{id}/lida")]
    public IActionResult MarkAsRead(Guid id)
    {
        var notif = Notifications.FirstOrDefault(n => n.Id == id);
        if (notif == null)
            throw new Exception("Notificação não encontrada.");

        notif.IsRead = true;
        return Ok(notif);
    }

    [HttpPatch("api/notificacoes/marcar-todas-lidas")]
    public IActionResult MarkAllAsRead()
    {
        var userId = GetUserId();
        foreach (var n in Notifications.Where(n => n.UserId == userId))
            n.IsRead = true;

        return Ok(new { message = "Todas as notificações foram marcadas como lidas." });
    }

    [HttpDelete("api/notificacoes/{id}")]
    public IActionResult DeleteNotification(Guid id)
    {
        var notif = Notifications.FirstOrDefault(n => n.Id == id);
        if (notif == null)
            throw new Exception("Notificação não encontrada.");

        Notifications.Remove(notif);
        return NoContent();
    }

    [HttpGet("api/usuarios/me/preferencias-notificacao")]
    public IActionResult GetPreferences()
    {
        return Ok(Preferences);
    }

    [HttpPut("api/usuarios/me/preferencias-notificacao")]
    public IActionResult UpdatePreferences([FromBody] NotificationPreferencesDto request)
    {
        Preferences = request;
        return Ok(Preferences);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationPreferencesDto
{
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
}
