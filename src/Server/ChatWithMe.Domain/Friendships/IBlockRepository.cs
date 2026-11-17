namespace ChatWithMe.Domain.Friendships;

public interface IBlockRepository
{
    /// <summary>blockerId 是否拉黑了 blockedId。</summary>
    Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken);

    /// <summary>两人之间是否存在任一方向的拉黑。</summary>
    Task<bool> IsBlockedEitherWayAsync(Guid userA, Guid userB, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserBlock>> ListByBlockerAsync(Guid blockerId, CancellationToken cancellationToken);

    Task AddAsync(UserBlock block, CancellationToken cancellationToken);

    Task RemoveAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
