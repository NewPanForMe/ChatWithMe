using ChatWithMe.Api.Contracts;
using ChatWithMe.Application.Admin;
using ChatWithMe.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

/// <summary>管理后台登录：与聊天用户的 /api/auth 完全独立，账号来自 admin_users。</summary>
[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(IAdminAuthAppService adminAuthAppService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> LoginAsync(
        [FromBody] AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await adminAuthAppService.LoginAsync(request, cancellationToken);

            return Ok(new AuthSessionResponse(
                tokens.AccessToken,
                tokens.TokenType,
                tokens.AccessTokenExpiresAtUtc,
                tokens.User));
        }
        catch (AuthenticationFailedException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = exception.Message,
            });
        }
    }
}
