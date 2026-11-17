namespace ChatWithMe.Domain.Friendships;

/// <summary>单向好友关系行：好友关系由两行组成（A→B、B→A），备注各存各的。</summary>
public sealed class Friendship
{
    public const int MaxRemarkLength = 50;

    private Friendship()
    {
    }

    public Friendship(Guid userId, Guid friendId, DateTime createdAtUtc)
    {
        if (userId == Guid.Empty || friendId == Guid.Empty)
        {
            throw new ArgumentException("好友关系双方标识不能为空。");
        }

        if (userId == friendId)
        {
            throw new ArgumentException("不能把自己加为好友。", nameof(friendId));
        }

        UserId = userId;
        FriendId = friendId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid FriendId { get; private set; }

    public string? Remark { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void SetRemark(string? remark)
    {
        if (remark is null)
        {
            Remark = null;
            return;
        }

        var trimmed = remark.Trim();

        if (trimmed.Length == 0)
        {
            Remark = null;
            return;
        }

        if (trimmed.Length > MaxRemarkLength)
        {
            throw new ArgumentException("好友备注长度不能超过 " + MaxRemarkLength + " 个字符。", nameof(remark));
        }

        Remark = trimmed;
    }
}
