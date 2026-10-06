using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class NotificationsController : ControllerBase
{
    [HttpGet("api/notificacoes")] public IActionResult GetMyNotifications() => StatusCode(501);
    [HttpPatch("api/notificacoes/{id}/lida")] public IActionResult MarkAsRead(Guid id) => StatusCode(501);
    [HttpPatch("api/notificacoes/marcar-todas-lidas")] public IActionResult MarkAllAsRead() => StatusCode(501);
    [HttpDelete("api/notificacoes/{id}")] public IActionResult DeleteNotification(Guid id) => StatusCode(501);
    [HttpGet("api/usuarios/me/preferencias-notificacao")] public IActionResult GetPreferences() => StatusCode(501);
    [HttpPut("api/usuarios/me/preferencias-notificacao")] public IActionResult UpdatePreferences() => StatusCode(501);
}
