using ChatWithMe.Domain.Friendships;

namespace ChatWithMe.Application.Friendships;

public sealed record UserSummaryDto(Guid Id, string Email, string NickName);

public sealed record SendFriendRequestRequest(Guid AddresseeId, string? Message);

public sealed record SetFriendRemarkRequest(string? Remark);

public sealed record FriendRequestDto(
    Guid Id,
    Guid RequesterId,
    string RequesterEmail,
    string RequesterNickName,
    Guid AddresseeId,
    string AddresseeEmail,
    string AddresseeNickName,
    string? Message,
    FriendRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime? RespondedAtUtc);

public sealed record FriendDto(Guid UserId, string Email, string NickName, string? Remark, DateTime CreatedAtUtc);

public sealed record BlockedUserDto(Guid UserId, string Email, string NickName, DateTime BlockedAtUtc);
