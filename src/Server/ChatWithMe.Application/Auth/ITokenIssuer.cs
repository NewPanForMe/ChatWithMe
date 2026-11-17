using ChatWithMe.Domain.Users;

namespace ChatWithMe.Application.Auth;

public interface ITokenIssuer
{
    IssuedTokenPair Issue(Guid userId, string email, UserRole role, int sessionVersion, string principalType);
}

public sealed record IssuedTokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
