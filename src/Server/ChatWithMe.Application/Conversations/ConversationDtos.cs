using ChatWithMe.Domain.Conversations;

namespace ChatWithMe.Application.Conversations;

public sealed record CreateConversationRequest(ConversationType Type, Guid? PeerUserId, string? Title);

public sealed record ConversationMemberDto(
    Guid UserId,
    ConversationMemberRole Role,
    DateTime JoinedAtUtc,
    DateTime? LastReadAtUtc,
    string? LastReadMessageId);

public sealed record ConversationDto(
    Guid Id,
    ConversationType Type,
    string? Title,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    IReadOnlyList<ConversationMemberDto> Members);
