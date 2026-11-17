using System.Globalization;
using ChatWithMe.Application.Auth;
using ChatWithMe.Domain.Admin;
using ChatWithMe.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Middleware;

/// <summary>
/// 校验访问令牌里的会话版本与数据库一致。管理员踢人 / 禁用后台账号后会话版本自增，
/// 旧令牌即使未过期也会在下一次请求时被拒绝（立即生效，不等 15 分钟）。
/// 管理端令牌（typ=admin）查 admin_users，聊天用户令牌（typ=user）查 users，两套账号互不通用。
/// </summary>
public sealed class SessionVersionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserRepository userRepository,
        IAdminUserRepository adminUserRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst("sub")?.Value;
            var sessionVersionClaim = context.User.FindFirst(AuthClaimNames.SessionVersion)?.Value;
            var principalType = context.User.FindFirst(AuthClaimNames.PrincipalType)?.Value;

            if (Guid.TryParse(subject, out var subjectId)
                && int.TryParse(sessionVersionClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tokenVersion))
            {
                var valid = principalType == AuthClaimNames.PrincipalAdmin
                    ? await IsAdminSessionValidAsync(adminUserRepository, subjectId, tokenVersion, context.RequestAborted)
                    : await IsUserSessionValidAsync(userRepository, subjectId, tokenVersion, context.RequestAborted);

                if (!valid)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "会话已失效",
                        Detail = "该账号已登出或被管理员强制下线，请重新登录。",
                    });

                    return;
                }
            }
        }

        await next(context);
    }

    private static async Task<bool> IsUserSessionValidAsync(
        IUserRepository users,
        Guid userId,
        int tokenVersion,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);

        return user is not null && user.SessionVersion == tokenVersion;
    }

    private static async Task<bool> IsAdminSessionValidAsync(
        IAdminUserRepository adminUsers,
        Guid adminId,
        int tokenVersion,
        CancellationToken cancellationToken)
    {
        var admin = await adminUsers.GetByIdAsync(adminId, cancellationToken);

        return admin is not null && admin.IsActive && admin.SessionVersion == tokenVersion;
    }
}
