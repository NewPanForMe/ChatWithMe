using ChatWithMe.Application.Messages;

namespace ChatWithMe.Api.Contracts;

/// <summary>
/// 消息分页响应。nextBefore 是翻页游标（配置时区文本，保留毫秒），直接回传给 before 即可。
/// </summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageDto> Items, string? NextBefore);
