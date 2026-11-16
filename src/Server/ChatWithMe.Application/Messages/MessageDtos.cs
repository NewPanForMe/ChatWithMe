namespace ChatWithMe.Application.Messages;

public sealed record SendMessageRequest(Guid ConversationId, string Content);

public sealed record MessageDto(Guid Id, Guid ConversationId, Guid SenderId, string Content, DateTime SentAtUtc);

/// <summary>
/// 消息分页结果。NextBeforeUtc 是翻页游标（不是展示时间），保留毫秒精度以免同一秒内的消息被漏掉；
/// 输出边界再由 API 层格式化为配置时区的文本。
/// </summary>
public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, DateTime? NextBeforeUtc);
