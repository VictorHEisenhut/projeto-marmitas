using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;

namespace Marmitas.Application.Users;

public static class UserMapper
{
    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Roles = user.GetRoleNames()
        };
    }
}

