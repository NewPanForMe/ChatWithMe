using ChatWithMe.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Messages;

public sealed class MessageAppService : IMessageAppService
{
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 100;

    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<MessageAppService> _logger;

    public MessageAppService(IMessageRepository messageRepository, ILogger<MessageAppService> logger)
    {
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = Message.Send(request.ConversationId, senderId, request.Content);

        await _messageRepository.AddAsync(message, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("消息已写入 MongoDB：{MessageId} 会话 {ConversationId}", message.Id, message.ConversationId);

        return ToDto(message);
    }

    public async Task<MessagePageDto> GetAsync(Guid conversationId, DateTime? beforeUtc, int limit, CancellationToken cancellationToken)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("会话标识不能为空。", nameof(conversationId));
        }

        var pageSize = limit <= 0 ? DefaultPageSize : Math.Min(limit, MaxPageSize);
        var normalizedBefore = beforeUtc?.ToUniversalTime();
        var messages = await _messageRepository
            .GetByConversationAsync(conversationId, normalizedBefore, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = messages.Select(ToDto).ToList();

        return new MessagePageDto(items, items.Count == 0 ? null : items[^1].SentAtUtc);
    }

    private static MessageDto ToDto(Message message) =>
        new(message.Id, message.ConversationId, message.SenderId, message.Content, message.SentAtUtc);
}
