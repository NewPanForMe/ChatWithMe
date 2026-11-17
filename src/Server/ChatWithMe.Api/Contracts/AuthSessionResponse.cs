using ChatWithMe.Application.Users;

namespace ChatWithMe.Api.Contracts;

/// <summary>
/// 登录 / 刷新的响应：只含 access token 与用户信息。
/// refresh token 通过 HttpOnly Cookie 下发，不出现在响应体里。
/// </summary>
public sealed record AuthSessionResponse(
    string AccessToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    UserDto User);
