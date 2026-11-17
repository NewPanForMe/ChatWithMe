using ChatWithMe.Domain.Messages;

namespace ChatWithMe.Application.Messages;

public interface IMessageAppService
{
    Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 拉取会话消息：仅会话成员可读，管理员可跨会话读取（审计用）。
    /// <paramref name="before"/> 为 <c>(SentAtUtc, Id)</c> 复合游标，null 表示第一页。
    /// </summary>
    Task<MessagePageDto> GetAsync(Guid userId, Guid conversationId, MessageCursor? before, int limit, CancellationToken cancellationToken);
}
