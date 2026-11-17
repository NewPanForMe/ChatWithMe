namespace ChatWithMe.Domain.Conversations;

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversation>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Conversation?> FindDirectAsync(Guid userA, Guid userB, CancellationToken cancellationToken);

    Task<ConversationMember?> GetMembershipAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
