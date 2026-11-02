using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users;
using Marmitas.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IRegisterUserService _registerUser;
    private readonly ILoginService _login;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthController(
        IRegisterUserService registerUser,
        ILoginService login,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _registerUser = registerUser;
        _login = login;
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _registerUser.ExecuteAsync(request);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _login.ExecuteAsync(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logout realizado com sucesso." });
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        Marmitas.Domain.Entities.User? user = null;

        if (Guid.TryParse(subClaim, out var userId))
        {
            user = await _users.GetByIdAsync(userId);
        }
        else if (!string.IsNullOrWhiteSpace(request?.Email))
        {
            user = await _users.GetByEmailAsync(request.Email.Trim().ToLower());
        }

        if (user == null)
            throw new Exception("Usuário não identificado para renovar o token.");

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = UserMapper.ToResponse(user)
        });
    }

    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> EsqueciSenha([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new Exception("Informe o e-mail da conta.");

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLower());
        if (user == null)
            throw new Exception("Usuário não encontrado com este e-mail.");

        return Ok(new
        {
            message = "Solicitação de redefinição de senha registrada com sucesso.",
            email = user.Email
        });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new Exception("Informe o e-mail da conta.");

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
            throw new Exception("A nova senha deve ter pelo menos 8 caracteres.");

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLower());
        if (user == null)
            throw new Exception("Usuário não encontrado.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _users.SaveChangesAsync();

        return Ok(new { message = "Senha atualizada com sucesso." });
    }
}
