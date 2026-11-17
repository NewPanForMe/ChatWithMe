using ChatWithMe.Domain.Common;

namespace ChatWithMe.Domain.Users;

public sealed class User : Entity
{
    private User()
    {
    }

    private User(Guid id, Email email, string nickName, string passwordHash, UserRole role) : base(id)
    {
        Email = email;
        NickName = nickName;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Email Email { get; private set; } = null!;

    public string NickName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; } = UserRole.Member;

    /// <summary>会话版本：每次被管理员踢下线/强制下线时自增，旧访问令牌据此立即失效。</summary>
    public int SessionVersion { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static User Register(string email, string nickName, string passwordHash, UserRole role = UserRole.Member)
    {
        if (string.IsNullOrWhiteSpace(nickName))
        {
            throw new ArgumentException("昵称不能为空。", nameof(nickName));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("密码哈希不能为空。", nameof(passwordHash));
        }

        var trimmedNickName = nickName.Trim();

        if (trimmedNickName.Length > 50)
        {
            throw new ArgumentException("昵称长度不能超过 50 个字符。", nameof(nickName));
        }

        var user = new User(Guid.NewGuid(), Email.Create(email), trimmedNickName, passwordHash, role);
        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email.Value, user.NickName, user.CreatedAtUtc));
        return user;
    }

    public void Rename(string nickName)
    {
        if (string.IsNullOrWhiteSpace(nickName))
        {
            throw new ArgumentException("昵称不能为空。", nameof(nickName));
        }

        NickName = nickName.Trim();
    }

    /// <summary>强制下线：自增会话版本，使该用户已签发的所有访问令牌立即失效。</summary>
    public void InvalidateSessions() => SessionVersion++;

    /// <summary>提升为管理员（仅在引导首个管理员时使用）。</summary>
    public void PromoteToAdmin() => Role = UserRole.Admin;
}
