using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface ITokenService
{

    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}

