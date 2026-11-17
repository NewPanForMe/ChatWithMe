namespace ChatWithMe.Domain.Friendships;

public interface IFriendshipRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid friendId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Friendship>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Friendship?> GetAsync(Guid userId, Guid friendId, CancellationToken cancellationToken);

    Task AddAsync(Friendship friendship, CancellationToken cancellationToken);

    Task RemovePairAsync(Guid userA, Guid userB, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
