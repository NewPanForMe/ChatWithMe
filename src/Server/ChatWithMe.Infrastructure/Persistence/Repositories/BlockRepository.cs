using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class BlockRepository : IBlockRepository
{
    private readonly ChatWithMeDbContext _context;

    public BlockRepository(ChatWithMeDbContext context) => _context = context;

    public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken) =>
        _context.UserBlocks.AnyAsync(block => block.BlockerId == blockerId && block.BlockedId == blockedId, cancellationToken);

    public Task<bool> IsBlockedEitherWayAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        _context.UserBlocks.AnyAsync(
            block => (block.BlockerId == userA && block.BlockedId == userB)
                || (block.BlockerId == userB && block.BlockedId == userA),
            cancellationToken);

    public async Task<IReadOnlyList<UserBlock>> ListByBlockerAsync(Guid blockerId, CancellationToken cancellationToken) =>
        await _context.UserBlocks
            .AsNoTracking()
            .Where(block => block.BlockerId == blockerId)
            .OrderByDescending(block => block.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task AddAsync(UserBlock block, CancellationToken cancellationToken)
    {
        _context.UserBlocks.Add(block);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken)
    {
        var block = await _context.UserBlocks
            .FirstOrDefaultAsync(row => row.BlockerId == blockerId && row.BlockedId == blockedId, cancellationToken)
            .ConfigureAwait(false);

        if (block is not null)
        {
            _context.UserBlocks.Remove(block);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
