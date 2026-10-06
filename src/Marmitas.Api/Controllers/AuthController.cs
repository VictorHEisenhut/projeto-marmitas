using Marmitas.Application.Users;
using Marmitas.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IRegisterUserService _registerUser;
    private readonly ILoginService _login;

    public AuthController(IRegisterUserService registerUser, ILoginService login)
    {
        _registerUser = registerUser;
        _login = login;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _registerUser.ExecuteAsync(request);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _login.ExecuteAsync(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout() => StatusCode(501);

    [HttpPost("refresh-token")]
    public IActionResult RefreshToken() => StatusCode(501);

    [HttpPost("esqueci-senha")]
    public IActionResult EsqueciSenha() => StatusCode(501);

    [HttpPost("redefinir-senha")]
    public IActionResult RedefinirSenha() => StatusCode(501);
}
