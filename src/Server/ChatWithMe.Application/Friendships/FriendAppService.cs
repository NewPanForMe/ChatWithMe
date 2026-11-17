using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Friendships;

public sealed class FriendAppService : IFriendAppService
{
    private const int SearchLimit = 20;

    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IFriendRequestRepository _friendRequestRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;
    private readonly ILogger<FriendAppService> _logger;

    public FriendAppService(
        IFriendshipRepository friendshipRepository,
        IFriendRequestRepository friendRequestRepository,
        IBlockRepository blockRepository,
        IUserRepository userRepository,
        IClock clock,
        ILogger<FriendAppService> logger)
    {
        _friendshipRepository = friendshipRepository;
        _friendRequestRepository = friendRequestRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(Guid userId, string? keyword, CancellationToken cancellationToken)
    {
        var normalized = Paging.NormalizeKeyword(keyword);

        if (normalized is null)
        {
            return [];
        }

        var users = await _userRepository.ListAsync(0, SearchLimit, normalized, cancellationToken).ConfigureAwait(false);

        return users
            .Where(user => user.Id != userId)
            .Select(user => new UserSummaryDto(user.Id, user.Email.Value, user.NickName))
            .ToList();
    }

    public async Task<FriendRequestDto> SendRequestAsync(Guid userId, SendFriendRequestRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AddresseeId == userId)
        {
            throw new ArgumentException("不能添加自己为好友。", nameof(request));
        }

        _ = await _userRepository.GetByIdAsync(request.AddresseeId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("目标用户不存在。");

        if (await _blockRepository.IsBlockedEitherWayAsync(userId, request.AddresseeId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("存在拉黑关系，无法发送好友申请。");
        }

        if (await _friendshipRepository.ExistsAsync(userId, request.AddresseeId, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("你们已经是好友。");
        }

        if (await _friendRequestRepository.GetPendingBetweenAsync(userId, request.AddresseeId, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new ConflictException("已有待处理的好友申请，请等待对方处理。");
        }

        var friendRequest = FriendRequest.Create(userId, request.AddresseeId, request.Message);

        await _friendRequestRepository.AddAsync(friendRequest, cancellationToken).ConfigureAwait(false);
        await _friendRequestRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("好友申请已创建：{RequestId} {RequesterId} -> {AddresseeId}", friendRequest.Id, userId, request.AddresseeId);

        return (await ToDtosAsync([friendRequest], cancellationToken).ConfigureAwait(false))[0];
    }

    public async Task<IReadOnlyList<FriendRequestDto>> ListIncomingRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var requests = await _friendRequestRepository.ListIncomingPendingAsync(userId, cancellationToken).ConfigureAwait(false);

        return await ToDtosAsync(requests, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FriendRequestDto>> ListOutgoingRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var requests = await _friendRequestRepository.ListOutgoingAsync(userId, cancellationToken).ConfigureAwait(false);

        return await ToDtosAsync(requests, cancellationToken).ConfigureAwait(false);
    }

    public Task<FriendRequestDto> AcceptAsync(Guid userId, Guid requestId, CancellationToken cancellationToken) =>
        RespondAsync(userId, requestId, accept: true, cancellationToken);

    public Task<FriendRequestDto> RejectAsync(Guid userId, Guid requestId, CancellationToken cancellationToken) =>
        RespondAsync(userId, requestId, accept: false, cancellationToken);

    public async Task<IReadOnlyList<FriendDto>> ListFriendsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var friendships = await _friendshipRepository.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);

        if (friendships.Count == 0)
        {
            return [];
        }

        var users = await _userRepository
            .GetByIdsAsync(friendships.Select(friendship => friendship.FriendId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var lookup = users.ToDictionary(user => user.Id);

        return friendships
            .Where(friendship => lookup.ContainsKey(friendship.FriendId))
            .Select(friendship => new FriendDto(
                friendship.FriendId,
                lookup[friendship.FriendId].Email.Value,
                lookup[friendship.FriendId].NickName,
                friendship.Remark,
                friendship.CreatedAtUtc))
            .OrderBy(friend => friend.NickName, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<FriendDto> SetRemarkAsync(Guid userId, Guid friendId, string? remark, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetAsync(userId, friendId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("你们不是好友。");

        friendship.SetRemark(remark);
        await _friendshipRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var friend = await _userRepository.GetByIdAsync(friendId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("好友用户不存在。");

        return new FriendDto(friendId, friend.Email.Value, friend.NickName, friendship.Remark, friendship.CreatedAtUtc);
    }

    public async Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken)
    {
        if (!await _friendshipRepository.ExistsAsync(userId, friendId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("你们不是好友。");
        }

        await _friendshipRepository.RemovePairAsync(userId, friendId, cancellationToken).ConfigureAwait(false);
        await _friendshipRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("好友关系已删除：{UserId} <-> {FriendId}", userId, friendId);
    }

    public async Task BlockAsync(Guid userId, Guid targetId, CancellationToken cancellationToken)
    {
        if (targetId == userId)
        {
            throw new ArgumentException("不能拉黑自己。", nameof(targetId));
        }

        _ = await _userRepository.GetByIdAsync(targetId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("目标用户不存在。");

        if (await _blockRepository.IsBlockedAsync(userId, targetId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await _blockRepository.AddAsync(new UserBlock(userId, targetId, _clock.UtcNow), cancellationToken).ConfigureAwait(false);

        if (await _friendshipRepository.ExistsAsync(userId, targetId, cancellationToken).ConfigureAwait(false))
        {
            await _friendshipRepository.RemovePairAsync(userId, targetId, cancellationToken).ConfigureAwait(false);
        }

        await _blockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _friendshipRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("用户 {UserId} 拉黑了 {TargetId}", userId, targetId);
    }

    public async Task UnblockAsync(Guid userId, Guid targetId, CancellationToken cancellationToken)
    {
        await _blockRepository.RemoveAsync(userId, targetId, cancellationToken).ConfigureAwait(false);
        await _blockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BlockedUserDto>> ListBlockedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var blocks = await _blockRepository.ListByBlockerAsync(userId, cancellationToken).ConfigureAwait(false);

        if (blocks.Count == 0)
        {
            return [];
        }

        var users = await _userRepository
            .GetByIdsAsync(blocks.Select(block => block.BlockedId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var lookup = users.ToDictionary(user => user.Id);

        return blocks
            .Where(block => lookup.ContainsKey(block.BlockedId))
            .Select(block => new BlockedUserDto(
                block.BlockedId,
                lookup[block.BlockedId].Email.Value,
                lookup[block.BlockedId].NickName,
                block.CreatedAtUtc))
            .ToList();
    }

    private async Task<FriendRequestDto> RespondAsync(Guid userId, Guid requestId, bool accept, CancellationToken cancellationToken)
    {
        var request = await _friendRequestRepository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("好友申请不存在。");

        if (request.AddresseeId != userId)
        {
            throw new ForbiddenException("只有被邀请人可以处理该申请。");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            throw new ConflictException("该好友申请已处理。");
        }

        var now = _clock.UtcNow;

        if (accept)
        {
            request.Accept(now);

            await _friendshipRepository.AddAsync(new Friendship(request.AddresseeId, request.RequesterId, now), cancellationToken).ConfigureAwait(false);
            await _friendshipRepository.AddAsync(new Friendship(request.RequesterId, request.AddresseeId, now), cancellationToken).ConfigureAwait(false);
            await _friendshipRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            request.Reject(now);
        }

        await _friendRequestRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("好友申请 {RequestId} 已{Action}", requestId, accept ? "同意" : "拒绝");

        return (await ToDtosAsync([request], cancellationToken).ConfigureAwait(false))[0];
    }

    private async Task<IReadOnlyList<FriendRequestDto>> ToDtosAsync(
        IReadOnlyList<FriendRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return [];
        }

        var userIds = requests
            .SelectMany(request => new[] { request.RequesterId, request.AddresseeId })
            .Distinct()
            .ToList();

        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        var lookup = users.ToDictionary(user => user.Id);

        return requests
            .Select(request =>
            {
                var requester = lookup.GetValueOrDefault(request.RequesterId);
                var addressee = lookup.GetValueOrDefault(request.AddresseeId);

                return new FriendRequestDto(
                    request.Id,
                    request.RequesterId,
                    requester?.Email.Value ?? string.Empty,
                    requester?.NickName ?? "未知用户",
                    request.AddresseeId,
                    addressee?.Email.Value ?? string.Empty,
                    addressee?.NickName ?? "未知用户",
                    request.Message,
                    request.Status,
                    request.CreatedAtUtc,
                    request.RespondedAtUtc);
            })
            .ToList();
    }
}
