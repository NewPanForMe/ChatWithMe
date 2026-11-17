using ChatWithMe.Application.Common;
using ChatWithMe.Application.Users;
using ChatWithMe.Domain.Admin;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Admin;

public sealed record AdminAccountDto(Guid Id, string Email, string DisplayName, bool IsActive, DateTime CreatedAtUtc);

public sealed record CreateAdminAccountRequest(string Email, string DisplayName, string Password);

public interface IAdminAccountAppService
{
    Task<IReadOnlyList<AdminAccountDto>> ListAsync(CancellationToken cancellationToken);

    Task<AdminAccountDto> CreateAsync(CreateAdminAccountRequest request, CancellationToken cancellationToken);

    /// <summary>删除管理员账号；不能删除自己。</summary>
    Task DeleteAsync(Guid id, Guid currentAdminId, CancellationToken cancellationToken);
}

public sealed class AdminAccountAppService(
    IAdminUserRepository adminUsers,
    ILogger<AdminAccountAppService> logger) : IAdminAccountAppService
{
    private const int MinPasswordLength = 8;

    public async Task<IReadOnlyList<AdminAccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        var admins = await adminUsers.ListAsync(cancellationToken);

        return admins
            .Select(admin => new AdminAccountDto(admin.Id, admin.Email, admin.DisplayName, admin.IsActive, admin.CreatedAtUtc))
            .ToList();
    }

    public async Task<AdminAccountDto> CreateAsync(CreateAdminAccountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
        {
            throw new ArgumentException("管理员密码至少 " + MinPasswordLength + " 位。", nameof(request));
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await adminUsers.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("该邮箱已存在管理员账号。");
        }

        var admin = AdminUser.Create(email, request.DisplayName, PasswordHasher.Hash(request.Password));

        await adminUsers.AddAsync(admin, cancellationToken);
        await adminUsers.SaveChangesAsync(cancellationToken);

        logger.LogWarning("已创建管理员账号：{AdminId} {Email}", admin.Id, admin.Email);

        return new AdminAccountDto(admin.Id, admin.Email, admin.DisplayName, admin.IsActive, admin.CreatedAtUtc);
    }

    public async Task DeleteAsync(Guid id, Guid currentAdminId, CancellationToken cancellationToken)
    {
        if (id == currentAdminId)
        {
            throw new ConflictException("不能删除当前登录的管理员账号。");
        }

        var admin = await adminUsers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("管理员账号不存在。");

        await adminUsers.DeleteAsync(id, cancellationToken);

        logger.LogWarning("已删除管理员账号：{AdminId} {Email}", admin.Id, admin.Email);
    }
}
