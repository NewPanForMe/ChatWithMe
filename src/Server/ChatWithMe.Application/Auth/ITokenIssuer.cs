namespace ChatWithMe.Application.Auth;

public interface ITokenIssuer
{
    IssuedTokenPair Issue(Guid userId, string email);
}

public sealed record IssuedTokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
