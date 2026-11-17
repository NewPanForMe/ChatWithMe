using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class FriendRequestRepository : IFriendRequestRepository
{
    private readonly ChatWithMeDbContext _context;

    public FriendRequestRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(FriendRequest request, CancellationToken cancellationToken)
    {
        _context.FriendRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task<FriendRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.FriendRequests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<FriendRequest?> GetPendingBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        _context.FriendRequests.FirstOrDefaultAsync(
            request => request.Status == FriendRequestStatus.Pending
                && ((request.RequesterId == userA && request.AddresseeId == userB)
                    || (request.RequesterId == userB && request.AddresseeId == userA)),
            cancellationToken);

    public async Task<IReadOnlyList<FriendRequest>> ListIncomingPendingAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.FriendRequests
            .AsNoTracking()
            .Where(request => request.AddresseeId == userId && request.Status == FriendRequestStatus.Pending)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<FriendRequest>> ListOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.FriendRequests
            .AsNoTracking()
            .Where(request => request.RequesterId == userId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
