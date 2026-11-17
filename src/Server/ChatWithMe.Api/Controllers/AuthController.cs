using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Contracts;
using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthAppService authAppService, IOptions<AuthCookieOptions> cookieOptions) : ControllerBase
{
    private const string UnauthorizedTitle = "身份验证失败";

    private readonly AuthCookieOptions _cookie = cookieOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await authAppService.LoginAsync(request, cancellationToken);

            SetRefreshCookie(tokens);

            return Ok(ToResponse(tokens));
        }
        catch (AuthenticationFailedException exception)
        {
            return UnauthorizedProblem(exception.Message);
        }
    }

    /// <summary>
    /// 刷新：刷新令牌只从 HttpOnly Cookie 读取，轮换后写回新 Cookie。
    /// 响应体只带新的 access token，跨站脚本即使触发请求也读不到令牌。
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> RefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_cookie.Name];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return UnauthorizedProblem("缺少刷新令牌 Cookie（" + _cookie.Name + "），请重新登录。");
        }

        try
        {
            var tokens = await authAppService.RefreshAsync(new RefreshTokenRequest(refreshToken), cancellationToken);

            SetRefreshCookie(tokens);

            return Ok(ToResponse(tokens));
        }
        catch (AuthenticationFailedException exception)
        {
            ClearRefreshCookie();

            return UnauthorizedProblem(exception.Message);
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_cookie.Name];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await authAppService.LogoutAsync(new RefreshTokenRequest(refreshToken), cancellationToken);
        }

        ClearRefreshCookie();

        return NoContent();
    }

    private static AuthSessionResponse ToResponse(AuthTokenDto tokens) =>
        new(tokens.AccessToken, tokens.TokenType, tokens.AccessTokenExpiresAtUtc, tokens.User);

    private void SetRefreshCookie(AuthTokenDto tokens) =>
        Response.Cookies.Append(
            _cookie.Name,
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookie.Secure,
                SameSite = _cookie.SameSite,
                Path = _cookie.Path,
                Domain = string.IsNullOrWhiteSpace(_cookie.Domain) ? null : _cookie.Domain,
                Expires = new DateTimeOffset(DateTime.SpecifyKind(tokens.RefreshTokenExpiresAtUtc, DateTimeKind.Utc)),
                IsEssential = true,
            });

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(
            _cookie.Name,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookie.Secure,
                SameSite = _cookie.SameSite,
                Path = _cookie.Path,
                Domain = string.IsNullOrWhiteSpace(_cookie.Domain) ? null : _cookie.Domain,
            });

    private ObjectResult UnauthorizedProblem(string detail) => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = UnauthorizedTitle,
        Detail = detail,
    });
}
