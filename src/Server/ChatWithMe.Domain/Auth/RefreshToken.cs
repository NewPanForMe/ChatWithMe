namespace ChatWithMe.Domain.Auth;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("用户标识不能为空。", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("刷新令牌哈希不能为空。", nameof(tokenHash));
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("刷新令牌过期时间必须晚于当前时间。", nameof(expiresAtUtc));
        }

        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, expiresAtUtc);
    }

    public void Revoke(DateTime utcNow, string? replacedByTokenHash)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
