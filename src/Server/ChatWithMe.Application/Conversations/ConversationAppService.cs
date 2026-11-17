using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Conversations;

public sealed class ConversationAppService : IConversationAppService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ConversationAppService> _logger;

    public ConversationAppService(
        IConversationRepository conversationRepository,
        IBlockRepository blockRepository,
        IUserRepository userRepository,
        ILogger<ConversationAppService> logger)
    {
        _conversationRepository = conversationRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ConversationDto> CreateAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Type switch
        {
            ConversationType.Direct => await CreateDirectAsync(userId, request.PeerUserId, cancellationToken).ConfigureAwait(false),
            ConversationType.Group => await CreateGroupAsync(userId, request.Title, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException("不支持的会话类型：" + request.Type + "。", nameof(request)),
        };
    }

    public async Task<IReadOnlyList<ConversationDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var conversations = await _conversationRepository.ListForUserAsync(userId, cancellationToken).ConfigureAwait(false);

        return conversations.Select(ToDto).ToList();
    }

    public async Task<ConversationDto> GetAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("会话不存在。");

        if (!conversation.IsMember(userId) && !await IsAdminAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("不是该会话成员，无权查看。");
        }

        return ToDto(conversation);
    }

    private async Task<ConversationDto> CreateDirectAsync(Guid userId, Guid? peerUserId, CancellationToken cancellationToken)
    {
        if (peerUserId is null || peerUserId == Guid.Empty)
        {
            throw new ArgumentException("单聊需要提供对方用户标识 PeerUserId。", nameof(peerUserId));
        }

        if (peerUserId.Value == userId)
        {
            throw new ArgumentException("不能与自己建立单聊。", nameof(peerUserId));
        }

        _ = await _userRepository.GetByIdAsync(peerUserId.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("对方用户不存在。");

        if (await _blockRepository.IsBlockedEitherWayAsync(userId, peerUserId.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("存在拉黑关系，无法发起会话。");
        }

        var existing = await _conversationRepository.FindDirectAsync(userId, peerUserId.Value, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var conversation = Conversation.CreateDirect(userId, peerUserId.Value);

        await _conversationRepository.AddAsync(conversation, cancellationToken).ConfigureAwait(false);
        await _conversationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("已创建单聊会话 {ConversationId}：{UserA} <-> {UserB}", conversation.Id, userId, peerUserId.Value);

        return ToDto(conversation);
    }

    private async Task<ConversationDto> CreateGroupAsync(Guid userId, string? title, CancellationToken cancellationToken)
    {
        var conversation = Conversation.CreateGroup(title ?? string.Empty, userId);

        await _conversationRepository.AddAsync(conversation, cancellationToken).ConfigureAwait(false);
        await _conversationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("已创建群聊会话 {ConversationId}：{Title}，群主 {OwnerId}", conversation.Id, conversation.Title, userId);

        return ToDto(conversation);
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        return user?.Role == UserRole.Admin;
    }

    private static ConversationDto ToDto(Conversation conversation) => new(
        conversation.Id,
        conversation.Type,
        conversation.Title,
        conversation.CreatedByUserId,
        conversation.CreatedAtUtc,
        conversation.Members
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new ConversationMemberDto(
                member.UserId,
                member.Role,
                member.JoinedAtUtc,
                member.LastReadAtUtc,
                member.LastReadMessageId))
            .ToList());
}
