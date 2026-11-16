namespace ChatWithMe.Domain.Messages;

public sealed class Message
{
    public const int MaxContentLength = 4000;

    private Message()
    {
    }

    private Message(Guid id, Guid conversationId, Guid senderId, string content, DateTime sentAtUtc)
    {
        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        Content = content;
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAtUtc { get; private set; }

    public static Message Send(Guid conversationId, Guid senderId, string content)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("会话标识不能为空。", nameof(conversationId));
        }

        if (senderId == Guid.Empty)
        {
            throw new ArgumentException("发送者标识不能为空。", nameof(senderId));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(content));
        }

        var trimmed = content.Trim();

        if (trimmed.Length > MaxContentLength)
        {
            throw new ArgumentException("消息内容长度不能超过 " + MaxContentLength + " 个字符。", nameof(content));
        }

        return new Message(Guid.NewGuid(), conversationId, senderId, trimmed, DateTime.UtcNow);
    }

    public static Message Restore(Guid id, Guid conversationId, Guid senderId, string content, DateTime sentAtUtc) =>
        new(id, conversationId, senderId, content, ToUtc(sentAtUtc));

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
