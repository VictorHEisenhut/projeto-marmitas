using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;

namespace Marmitas.Application.Users;

public interface ILoginService
{
    Task<LoginResponse> ExecuteAsync(LoginRequest request);
}

public class LoginService : ILoginService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginService(IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new Exception("E-mail ou senha inválidos.");

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLower());
        if (user == null)
            throw new Exception("E-mail ou senha inválidos.");

        if (user.IsLockedOut())
            throw new Exception("Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em alguns minutos.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin();
            await _users.SaveChangesAsync();
            throw new Exception("E-mail ou senha inválidos.");
        }

        if (user.Status == UserStatus.Blocked)
            throw new Exception("Usuário bloqueado. Entre em contato com o suporte.");

        user.ResetFailedLogins();

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        var refreshTokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        await _users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _users.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt,
            User = UserMapper.ToResponse(user)
        };
    }
}
