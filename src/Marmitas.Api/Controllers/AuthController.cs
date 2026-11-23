using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;
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
    private readonly ICustomerProfileRepository _profiles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IHostEnvironment _environment;

    public AuthController(
        IRegisterUserService registerUser,
        ILoginService login,
        IUserRepository users,
        ICustomerProfileRepository profiles,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IHostEnvironment environment)
    {
        _registerUser = registerUser;
        _login = login;
        _users = users;
        _profiles = profiles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _environment = environment;
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
    public async Task<IActionResult> Logout()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(subClaim, out var userId))
        {
            await _users.RevokeUserRefreshTokensAsync(userId);
            await _users.SaveChangesAsync();
        }

        return Ok(new { message = "Logout realizado com sucesso." });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        User? user = null;
        RefreshToken? existingRefresh = null;

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            existingRefresh = await _users.GetRefreshTokenAsync(request.RefreshToken.Trim());
            if (existingRefresh == null || !existingRefresh.IsActive)
                throw new Exception("Refresh token inválido ou expirado.");

            user = await _users.GetByIdAsync(existingRefresh.UserId);
        }
        else
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(subClaim, out var userId))
            {
                user = await _users.GetByIdAsync(userId);
            }
            else if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(request?.Email))
            {
                user = await _users.GetByEmailAsync(request.Email.Trim().ToLower());
            }
        }

        if (user == null)
            throw new Exception("Sessão inválida para renovar o token.");

        var newRefreshValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        if (existingRefresh != null)
        {
            existingRefresh.RevokedAt = DateTime.UtcNow;
            existingRefresh.ReplacedByToken = newRefreshValue;
        }

        await _users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _users.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = newRefreshValue,
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

        var code = Random.Shared.Next(100000, 999999).ToString();
        await _users.AddPasswordResetTokenAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Email = user.Email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });

        await _profiles.AddNotificationAsync(new Notification
        {
            UserId = user.Id,
            Title = "Código de recuperação de senha",
            Message = $"Seu código de verificação para redefinir a senha é {code} (válido por 15 minutos).",
            IsRead = false
        });

        await _users.SaveChangesAsync();

        return Ok(new
        {
            message = "Solicitação de redefinição de senha registrada com sucesso.",
            email = user.Email,
            resetCode = _environment.IsProduction() ? null : code
        });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new Exception("Informe o e-mail da conta.");

        if (string.IsNullOrWhiteSpace(request.ResetCode))
            throw new Exception("O código de verificação é obrigatório.");

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
            throw new Exception("A nova senha deve ter pelo menos 8 caracteres.");

        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await _users.GetByEmailAsync(normalizedEmail);
        if (user == null)
            throw new Exception("Usuário não encontrado.");

        var tokenEntry = await _users.GetLatestPasswordResetTokenAsync(normalizedEmail);
        if (tokenEntry == null)
            throw new Exception("Nenhuma solicitação de redefinição ativa encontrada. Solicite um novo código.");

        if (tokenEntry.ExpiresAt < DateTime.UtcNow || tokenEntry.Attempts >= 3)
        {
            tokenEntry.UsedAt = DateTime.UtcNow;
            await _users.SaveChangesAsync();
            throw new Exception("O código de verificação expirou ou excedeu o limite de tentativas. Solicite um novo código.");
        }

        if (request.ResetCode.Trim() != tokenEntry.Code)
        {
            tokenEntry.Attempts++;
            await _users.SaveChangesAsync();
            throw new Exception("Código de verificação inválido.");
        }

        tokenEntry.UsedAt = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.ResetFailedLogins();
        await _users.RevokeUserRefreshTokensAsync(user.Id);
        await _users.SaveChangesAsync();

        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    [HttpPost("mfa/ativar")]
    [Authorize]
    public async Task<IActionResult> EnableMfa()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var userId))
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("Usuário não encontrado.");

        var secretCode = Random.Shared.Next(100000, 999999).ToString();
        user.MfaEnabled = true;
        user.MfaSecret = secretCode;
        await _users.SaveChangesAsync();

        return Ok(new
        {
            mfaEnabled = true,
            verificationCode = secretCode,
            message = "Autenticação em duas etapas (MFA) habilitada."
        });
    }

    [HttpPost("mfa/desativar")]
    [Authorize]
    public async Task<IActionResult> DisableMfa()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var userId))
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("Usuário não encontrado.");

        user.MfaEnabled = false;
        user.MfaSecret = null;
        await _users.SaveChangesAsync();

        return Ok(new
        {
            mfaEnabled = false,
            message = "Autenticação em duas etapas (MFA) desativada."
        });
    }
}
