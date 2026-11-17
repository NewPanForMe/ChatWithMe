using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class FriendshipRepository : IFriendshipRepository
{
    private readonly ChatWithMeDbContext _context;

    public FriendshipRepository(ChatWithMeDbContext context) => _context = context;

    public Task<bool> ExistsAsync(Guid userId, Guid friendId, CancellationToken cancellationToken) =>
        _context.Friendships.AnyAsync(friendship => friendship.UserId == userId && friendship.FriendId == friendId, cancellationToken);

    public async Task<IReadOnlyList<Friendship>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.UserId == userId)
            .OrderByDescending(friendship => friendship.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Friendship?> GetAsync(Guid userId, Guid friendId, CancellationToken cancellationToken) =>
        _context.Friendships.FirstOrDefaultAsync(friendship => friendship.UserId == userId && friendship.FriendId == friendId, cancellationToken);

    public Task AddAsync(Friendship friendship, CancellationToken cancellationToken)
    {
        _context.Friendships.Add(friendship);
        return Task.CompletedTask;
    }

    public async Task RemovePairAsync(Guid userA, Guid userB, CancellationToken cancellationToken)
    {
        var rows = await _context.Friendships
            .Where(friendship =>
                (friendship.UserId == userA && friendship.FriendId == userB)
                || (friendship.UserId == userB && friendship.FriendId == userA))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _context.Friendships.RemoveRange(rows);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
