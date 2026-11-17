using ChatWithMe.Domain.Common;

namespace ChatWithMe.Domain.Admin;

/// <summary>
/// 管理后台账号：与聊天用户（<see cref="Users.User"/>）完全分离，两套账号不能互相登录。
/// </summary>
public sealed class AdminUser : Entity
{
    private AdminUser()
    {
    }

    private AdminUser(Guid id, string email, string displayName, string passwordHash) : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>会话版本：踢下线/禁用时自增，旧访问令牌立即失效。</summary>
    public int SessionVersion { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static AdminUser Create(string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("管理员邮箱格式不正确。", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("管理员名称不能为空。", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("密码哈希不能为空。", nameof(passwordHash));
        }

        return new AdminUser(Guid.NewGuid(), email.Trim().ToLowerInvariant(), displayName.Trim(), passwordHash);
    }

    public void InvalidateSessions() => SessionVersion++;

    public void Disable() => IsActive = false;
}
