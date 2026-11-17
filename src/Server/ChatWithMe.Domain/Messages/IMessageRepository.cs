namespace ChatWithMe.Domain.Messages;

public interface IMessageRepository
{
    /// <summary>
    /// 写入消息。带 ClientMessageId 时按幂等键处理：已存在则返回已存储的消息，不重复插入。
    /// </summary>
    Task<Message> AddAsync(Message message, CancellationToken cancellationToken);

    Task<Message?> GetByClientMessageIdAsync(Guid conversationId, string clientMessageId, CancellationToken cancellationToken);

    /// <summary>
    /// 按 <c>(SentAtUtc, Id)</c> 倒序取会话消息；<paramref name="before"/> 为 null 时取第一页。
    /// </summary>
    Task<IReadOnlyList<Message>> GetByConversationAsync(Guid conversationId, MessageCursor? before, int limit, CancellationToken cancellationToken);
}
