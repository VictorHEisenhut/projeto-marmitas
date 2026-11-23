using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Constants;
using Marmitas.Domain.Entities;

namespace Marmitas.Application.Users;

public interface IRegisterUserService
{
    Task<UserResponse> ExecuteAsync(RegisterRequest request);
}

public class RegisterUserService : IRegisterUserService
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> ExecuteAsync(RegisterRequest request)
    {
        Validate(request);

        var email = request.Email.Trim().ToLower();
        if (await _users.EmailExistsAsync(email))
        {
            throw new Exception("Já existe uma conta com este e-mail.");
        }

        var requestedRole = request.Role?.Trim().ToLower() switch
        {
            "restaurant_owner" => RoleNames.RestaurantOwner,
            "restaurant_staff" => RoleNames.RestaurantStaff,
            "driver" => RoleNames.Driver,
            _ => RoleNames.Customer
        };

        var role = await _users.GetRoleByNameAsync(requestedRole);
        if (role == null)
        {
            role = new Role(requestedRole, "Perfil da plataforma");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, email, request.Phone, passwordHash);
        user.AddRole(role);

        await _users.AddAsync(user);
        await _users.AddUserConsentAsync(new UserConsent
        {
            UserId = user.Id,
            Purpose = "TermsAndPrivacy",
            TermsVersion = request.TermsVersion ?? "v1.0",
            IsGranted = request.TermsAccepted,
            ConsentedAt = DateTime.UtcNow
        });
        await _users.SaveChangesAsync();

        return UserMapper.ToResponse(user);
    }

    private static void Validate(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome é obrigatório.");
        if (request.Name.Trim().Length > 150)
            throw new Exception("O nome deve ter no máximo 150 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new Exception("O e-mail é obrigatório.");
        if (!IsValidEmail(request.Email.Trim()))
            throw new Exception("O e-mail informado é inválido.");

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinPasswordLength)
            throw new Exception($"A senha deve ter pelo menos {MinPasswordLength} caracteres.");

        if (request.Phone != null && request.Phone.Length > 20)
            throw new Exception("O telefone deve ter no máximo 20 caracteres.");
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length > 254 || email.Contains(' '))
            return false;

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@'))
            return false;

        var domain = email.Substring(at + 1);
        var dot = domain.LastIndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }
}
