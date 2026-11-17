namespace ChatWithMe.Domain.Friendships;

public interface IFriendRequestRepository
{
    Task AddAsync(FriendRequest request, CancellationToken cancellationToken);

    Task<FriendRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FriendRequest?> GetPendingBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendRequest>> ListIncomingPendingAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendRequest>> ListOutgoingAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
