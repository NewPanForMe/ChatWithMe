namespace ChatWithMe.Application.Conversations;

public interface IConversationAppService
{
    /// <summary>创建会话：单聊会复用已存在的两人会话；群聊创建时仅包含创建者（成员邀请需对方确认，见后续迭代）。</summary>
    Task<ConversationDto> CreateAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationDto>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConversationDto> GetAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
}
