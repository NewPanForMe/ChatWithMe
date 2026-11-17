using ChatWithMe.Application.Messages;

namespace ChatWithMe.Api.Contracts;

/// <summary>
/// 消息分页响应。<c>nextBefore</c> 是 (SentAtUtc, Id) 复合游标的文本形式
/// （配置时区毫秒时间 + 消息 Id，分隔符 <c>|</c>），原样回传给 <c>before</c> 即可。
/// </summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageDto> Items, string? NextBefore);
