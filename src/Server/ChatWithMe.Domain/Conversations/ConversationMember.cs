namespace ChatWithMe.Domain.Conversations;

public sealed class ConversationMember
{
    private ConversationMember()
    {
    }

    internal ConversationMember(Guid conversationId, Guid userId, ConversationMemberRole role, DateTime joinedAtUtc)
    {
        ConversationId = conversationId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid ConversationId { get; private set; }

    public Guid UserId { get; private set; }

    public ConversationMemberRole Role { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>会话级已读位置：最后一条已读消息的标识与时间。</summary>
    public string? LastReadMessageId { get; private set; }

    public DateTime? LastReadAtUtc { get; private set; }

    public DateTime? MutedUntilUtc { get; private set; }

    public void MarkRead(string? messageId, DateTime readAtUtc)
    {
        if (readAtUtc.Kind != DateTimeKind.Utc)
        {
            readAtUtc = readAtUtc.ToUniversalTime();
        }

        LastReadMessageId = messageId;
        LastReadAtUtc = readAtUtc;
    }
}
