namespace ChatWithMe.Application.Messages;

public interface IMessageAppService
{
    Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken);

    Task<MessagePageDto> GetAsync(Guid conversationId, DateTime? beforeUtc, int limit, CancellationToken cancellationToken);
}
