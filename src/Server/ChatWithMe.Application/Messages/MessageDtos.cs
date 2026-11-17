using ChatWithMe.Domain.Messages;

namespace ChatWithMe.Application.Messages;

public sealed record SendMessageRequest(Guid ConversationId, string Content, string? ClientMessageId = null);

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    string? ClientMessageId,
    DateTime SentAtUtc);

/// <summary>
/// 消息分页结果。<see cref="Next"/> 是 <c>(SentAtUtc, Id)</c> 复合游标（不是展示时间），
/// 由 API 层编码成配置时区的文本后作为 <c>nextBefore</c> 返回。
/// </summary>
public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, MessageCursor? Next);
