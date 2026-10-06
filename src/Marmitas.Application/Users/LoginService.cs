using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users.Dtos;
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

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new Exception("E-mail ou senha inválidos.");

        if (user.Status == UserStatus.Blocked)
        {
            throw new Exception("Usuário bloqueado. Entre em contato com o suporte.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = UserMapper.ToResponse(user)
        };
    }
}

