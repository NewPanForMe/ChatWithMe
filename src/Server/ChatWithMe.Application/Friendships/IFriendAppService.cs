namespace ChatWithMe.Application.Friendships;

public interface IFriendAppService
{
    Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(Guid userId, string? keyword, CancellationToken cancellationToken);

    Task<FriendRequestDto> SendRequestAsync(Guid userId, SendFriendRequestRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendRequestDto>> ListIncomingRequestsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendRequestDto>> ListOutgoingRequestsAsync(Guid userId, CancellationToken cancellationToken);

    Task<FriendRequestDto> AcceptAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);

    Task<FriendRequestDto> RejectAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendDto>> ListFriendsAsync(Guid userId, CancellationToken cancellationToken);

    Task<FriendDto> SetRemarkAsync(Guid userId, Guid friendId, string? remark, CancellationToken cancellationToken);

    Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken);

    Task BlockAsync(Guid userId, Guid targetId, CancellationToken cancellationToken);

    Task UnblockAsync(Guid userId, Guid targetId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlockedUserDto>> ListBlockedAsync(Guid userId, CancellationToken cancellationToken);
}
