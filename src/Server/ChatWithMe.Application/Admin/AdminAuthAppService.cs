using ChatWithMe.Application.Auth;
using ChatWithMe.Domain.Admin;
using ChatWithMe.Domain.Users;
using ChatWithMe.Application.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Admin;

public sealed record AdminLoginRequest(string Email, string Password);

public interface IAdminAuthAppService
{
    Task<AuthTokenDto> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken);
}

public sealed class AdminAuthAppService(
    IAdminUserRepository adminUsers,
    ITokenIssuer tokenIssuer,
    ILogger<AdminAuthAppService> logger) : IAdminAuthAppService
{
    private const string InvalidCredentialsMessage = "管理员邮箱或密码不正确。";

    public async Task<AuthTokenDto> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var admin = email is null ? null : await adminUsers.GetByEmailAsync(email, cancellationToken);

        if (admin is null || !admin.IsActive || !PasswordHasher.Verify(request.Password, admin.PasswordHash))
        {
            logger.LogWarning("管理员登录失败：{Email}", request.Email);

            throw new ChatWithMe.Application.Common.InvalidCredentialsException(InvalidCredentialsMessage);
        }

        var issued = tokenIssuer.Issue(
            admin.Id,
            admin.Email,
            UserRole.Admin,
            admin.SessionVersion,
            AuthClaimNames.PrincipalAdmin);

        logger.LogInformation("管理员登录成功：{AdminId} {Email}", admin.Id, admin.Email);

        return new AuthTokenDto(
            issued.AccessToken,
            string.Empty,
            "Bearer",
            issued.AccessTokenExpiresAtUtc,
            issued.AccessTokenExpiresAtUtc,
            new UserDto(admin.Id, admin.Email, admin.DisplayName, admin.CreatedAtUtc));
    }
}
