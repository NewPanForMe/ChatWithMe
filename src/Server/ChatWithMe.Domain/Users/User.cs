using ChatWithMe.Domain.Common;

namespace ChatWithMe.Domain.Users;

public sealed class User : Entity
{
    private User()
    {
    }

    private User(Guid id, Email email, string nickName, string passwordHash) : base(id)
    {
        Email = email;
        NickName = nickName;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Email Email { get; private set; } = null!;

    public string NickName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static User Register(string email, string nickName, string passwordHash)
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

        var user = new User(Guid.NewGuid(), Email.Create(email), trimmedNickName, passwordHash);
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
}
