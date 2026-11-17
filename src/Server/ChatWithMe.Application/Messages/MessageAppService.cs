using ChatWithMe.Application.Common;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Domain.Messages;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Messages;

public sealed class MessageAppService : IMessageAppService
{
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 100;

    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChatNotifier _chatNotifier;
    private readonly ILogger<MessageAppService> _logger;

    public MessageAppService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IBlockRepository blockRepository,
        IUserRepository userRepository,
        IChatNotifier chatNotifier,
        ILogger<MessageAppService> logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _chatNotifier = chatNotifier;
        _logger = logger;
    }

    public async Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("会话不存在。");

        if (!conversation.IsMember(senderId))
        {
            throw new ForbiddenException("不是该会话成员，不能发送消息。");
        }

        if (conversation.Type == ConversationType.Direct)
        {
            var peer = conversation.Members.FirstOrDefault(member => member.UserId != senderId);

            if (peer is not null && await _blockRepository.IsBlockedAsync(peer.UserId, senderId, cancellationToken).ConfigureAwait(false))
            {
                throw new ForbiddenException("对方已将你拉黑，无法发送消息。");
            }
        }

        var message = Message.Send(request.ConversationId, senderId, request.Content, request.ClientMessageId);
        var stored = await _messageRepository.AddAsync(message, cancellationToken).ConfigureAwait(false);
        var dto = ToDto(stored);

        if (stored.Id == message.Id)
        {
            _logger.LogInformation("消息已写入 MongoDB：{MessageId} 会话 {ConversationId}", stored.Id, stored.ConversationId);

            await _chatNotifier.MessageReceivedAsync(dto, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("幂等命中，返回已存在的消息：{MessageId} 会话 {ConversationId}", stored.Id, stored.ConversationId);
        }

        return dto;
    }

    public async Task<MessagePageDto> GetAsync(Guid userId, Guid conversationId, MessageCursor? before, int limit, CancellationToken cancellationToken)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("会话标识不能为空。", nameof(conversationId));
        }

        await EnsureCanReadAsync(conversationId, userId, cancellationToken).ConfigureAwait(false);

        var pageSize = limit <= 0 ? DefaultPageSize : Math.Min(limit, MaxPageSize);
        var messages = await _messageRepository
            .GetByConversationAsync(conversationId, before, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = messages.Select(ToDto).ToList();
        var next = messages.Count == 0 ? null : MessageCursor.After(messages[^1]);

        return new MessagePageDto(items, next);
    }

    /// <summary>读取权限：成员可读；管理员可读任意会话（审计需要）。</summary>
    private async Task EnsureCanReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("会话不存在。");

        if (conversation.IsMember(userId))
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (user?.Role != UserRole.Admin)
        {
            throw new ForbiddenException("不是该会话成员，无权拉取会话消息。");
        }
    }

    private static MessageDto ToDto(Message message) =>
        new(message.Id, message.ConversationId, message.SenderId, message.Content, message.ClientMessageId, message.SentAtUtc);
}
