using ChatWithMe.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ChatWithMeDbContext _context;

    public ConversationRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        _context.Conversations.Add(conversation);
        return Task.CompletedTask;
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Members)
            .Where(conversation => conversation.Members.Any(member => member.UserId == userId))
            .OrderByDescending(conversation => conversation.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Conversation?> FindDirectAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation => conversation.Type == ConversationType.Direct
                    && conversation.Members.Count == 2
                    && conversation.Members.Any(member => member.UserId == userA)
                    && conversation.Members.Any(member => member.UserId == userB),
                cancellationToken);

    public Task<ConversationMember?> GetMembershipAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken) =>
        _context.ConversationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                member => member.ConversationId == conversationId && member.UserId == userId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
