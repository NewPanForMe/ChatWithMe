namespace ChatWithMe.Domain.Messages;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> GetByConversationAsync(Guid conversationId, DateTime? beforeUtc, int limit, CancellationToken cancellationToken);
}
