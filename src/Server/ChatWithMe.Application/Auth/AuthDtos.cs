using ChatWithMe.Application.Users;

namespace ChatWithMe.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    UserDto User);
