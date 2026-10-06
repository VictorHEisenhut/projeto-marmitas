using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/status")]
[ApiController]
public class StatusController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new StatusResponse("ok", "v1", environment.EnvironmentName, DateTimeOffset.UtcNow));
}

public sealed record StatusResponse(string Status, string Version, string Environment, DateTimeOffset ServerTime);
