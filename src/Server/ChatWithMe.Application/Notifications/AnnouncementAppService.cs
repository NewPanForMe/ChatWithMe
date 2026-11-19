using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Notifications;

public sealed record AnnouncementDto(Guid Id, string Title, string Content, DateTime CreatedAtUtc);
public sealed record PublishAnnouncementRequest(string Title, string Content);
public sealed record PublishAnnouncementResult(AnnouncementDto Notification, bool RealtimeDelivered);

public interface IAnnouncementNotifier
{
    Task BroadcastAsync(AnnouncementDto notification, CancellationToken cancellationToken);
}

public sealed class AnnouncementAppService(IAnnouncementRepository repository, IAnnouncementNotifier notifier,
    IClock clock, ILogger<AnnouncementAppService> logger)
{
    public async Task<PublishAnnouncementResult> PublishAsync(Guid adminId, PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var item = Announcement.Create(adminId, request.Title, request.Content, clock.UtcNow);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        var dto = ToDto(item);
        try
        {
            await notifier.BroadcastAsync(dto, cancellationToken).ConfigureAwait(false);
            return new(dto, true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "通知 {NotificationId} 已保存，实时广播失败，客户端可通过列表补拉", item.Id);
            return new(dto, false);
        }
    }

    public async Task<PagedResult<AnnouncementDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var (current, size, skip) = Paging.Normalize(page, pageSize);
        var total = await repository.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await repository.ListAsync(skip, size, cancellationToken).ConfigureAwait(false);
        return new(items.Select(ToDto).ToList(), total, current, size);
    }

    public async Task<AnnouncementDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        ToDto(await repository.GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("通知不存在。"));

    private static AnnouncementDto ToDto(Announcement item) => new(item.Id, item.Title, item.Content, item.CreatedAtUtc);
}
