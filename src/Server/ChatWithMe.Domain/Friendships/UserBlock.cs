namespace ChatWithMe.Domain.Friendships;

/// <summary>拉黑关系：BlockerId 拉黑了 BlockedId。</summary>
public sealed class UserBlock
{
    private UserBlock()
    {
    }

    public UserBlock(Guid blockerId, Guid blockedId, DateTime createdAtUtc)
    {
        if (blockerId == Guid.Empty || blockedId == Guid.Empty)
        {
            throw new ArgumentException("拉黑关系双方标识不能为空。");
        }

        if (blockerId == blockedId)
        {
            throw new ArgumentException("不能拉黑自己。", nameof(blockedId));
        }

        BlockerId = blockerId;
        BlockedId = blockedId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid BlockerId { get; private set; }

    public Guid BlockedId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
