using ChatWithMe.Domain.Users;
using ChatWithMe.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ChatWithMeDbContext _context;

    public UserRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(user => user.Email.Value == email, cancellationToken);

    public Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(user => user.Email.Value == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idList = ids.ToList();

        return await _context.Users
            .AsNoTracking()
            .Where(user => idList.Contains(user.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken) =>
        await BuildQuery(keyword)
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenByDescending(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountAsync(string? keyword, CancellationToken cancellationToken) =>
        BuildQuery(keyword).CountAsync(cancellationToken);

    private IQueryable<User> BuildQuery(string? keyword)
    {
        var query = _context.Users.AsNoTracking();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var pattern = LikePatterns.Contains(keyword);

        return query.Where(user =>
            EF.Functions.ILike(user.Email.Value, pattern, LikePatterns.EscapeCharacter) ||
            EF.Functions.ILike(user.NickName, pattern, LikePatterns.EscapeCharacter));
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var conversationIds = await _context.ConversationMembers
            .Where(member => member.UserId == userId)
            .Select(member => member.ConversationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await _context.ConversationMembers
            .Where(member => member.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (conversationIds.Count > 0)
        {
            var emptyConversationIds = await _context.Conversations
                .Where(conversation => conversationIds.Contains(conversation.Id)
                    && !_context.ConversationMembers.Any(member => member.ConversationId == conversation.Id))
                .Select(conversation => conversation.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (emptyConversationIds.Count > 0)
            {
                await _context.Conversations
                    .Where(conversation => emptyConversationIds.Contains(conversation.Id))
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await _context.RefreshTokens.Where(token => token.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.Friendships.Where(friendship => friendship.UserId == userId || friendship.FriendId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.FriendRequests.Where(request => request.RequesterId == userId || request.AddresseeId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.UserBlocks.Where(block => block.BlockerId == userId || block.BlockedId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.Users.Where(user => user.Id == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
