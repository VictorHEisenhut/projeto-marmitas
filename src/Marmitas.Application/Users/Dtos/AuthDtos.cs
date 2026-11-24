namespace Marmitas.Application.Users.Dtos;

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool TermsAccepted { get; set; } = true;
    public string? TermsVersion { get; set; } = "v1.0";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = new();
}

public class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string>? Permissions { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? ResetCode { get; set; }
}

public class RefreshTokenRequest
{
    public string? Email { get; set; }
    public string? RefreshToken { get; set; }
}
