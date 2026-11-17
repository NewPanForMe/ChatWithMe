namespace ChatWithMe.Client.Models;

// 后端的时间统一输出 yyyy/MM/dd HH:mm:ss（配置时区文本），不是 ISO-8601，
// 所以这里一律用 string 接收，避免 System.Text.Json 解析失败。

public sealed record UserDto(Guid Id, string Email, string NickName, string CreatedAtUtc);

public sealed record AuthSessionDto(string AccessToken, string TokenType, string AccessTokenExpiresAtUtc, UserDto User);

public sealed record UserSummaryDto(Guid Id, string Email, string NickName);

public sealed record ConversationMemberDto(
    Guid UserId,
    string Role,
    string JoinedAtUtc,
    string? LastReadAtUtc,
    string? LastReadMessageId);

public sealed record ConversationDto(
    Guid Id,
    string Type,
    string? Title,
    Guid CreatedByUserId,
    string CreatedAtUtc,
    IReadOnlyList<ConversationMemberDto> Members);

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    string? ClientMessageId,
    string SentAtUtc);

public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, string? NextBefore);

public sealed record FriendDto(Guid UserId, string Email, string NickName, string? Remark, string CreatedAtUtc)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Remark) ? NickName : Remark;
}

public sealed record FriendRequestDto(
    Guid Id,
    Guid RequesterId,
    string RequesterEmail,
    string RequesterNickName,
    Guid AddresseeId,
    string AddresseeEmail,
    string AddresseeNickName,
    string? Message,
    string Status,
    string CreatedAtUtc,
    string? RespondedAtUtc);

public sealed record BlockedUserDto(Guid UserId, string Email, string NickName, string BlockedAtUtc);
