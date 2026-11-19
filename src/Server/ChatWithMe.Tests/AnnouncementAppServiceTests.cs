using ChatWithMe.Application.Notifications;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Notifications;
using ChatWithMe.Infrastructure.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class AnnouncementAppServiceTests
{
    [Fact]
    public async Task Publish_SavesBeforeBroadcastAndSupportsOfflineRead()
    {
        var repository = new MemoryAnnouncements();
        var notifier = new RecordingNotifier(repository);
        var service = Create(repository, notifier);
        var result = await service.PublishAsync(Guid.NewGuid(), new("标题", "完整正文"), default);
        Assert.True(result.RealtimeDelivered);
        Assert.Equal(result.Notification, Assert.Single(notifier.Sent));
        Assert.Equal(result.Notification, await service.GetAsync(result.Notification.Id, default));
        Assert.Equal(result.Notification, Assert.Single((await service.ListAsync(1, 20, default)).Items));
    }

    [Fact]
    public async Task BroadcastFailure_KeepsNotificationAndReportsPartialSuccess()
    {
        var repository = new MemoryAnnouncements();
        var service = Create(repository, new RecordingNotifier(repository) { Fail = true });
        var result = await service.PublishAsync(Guid.NewGuid(), new("标题", "内容"), default);
        Assert.False(result.RealtimeDelivered);
        Assert.Equal("内容", (await service.GetAsync(result.Notification.Id, default)).Content);
    }

    [Fact]
    public async Task SaveFailure_DoesNotBroadcast()
    {
        var repository = new MemoryAnnouncements { FailSave = true };
        var notifier = new RecordingNotifier(repository);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(repository, notifier)
            .PublishAsync(Guid.NewGuid(), new("标题", "内容"), default));
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task MissingDetail_ReturnsNotFound()
    {
        var repository = new MemoryAnnouncements();
        await Assert.ThrowsAsync<NotFoundException>(() => Create(repository, new RecordingNotifier(repository)).GetAsync(Guid.NewGuid(), default));
    }

    private static AnnouncementAppService Create(MemoryAnnouncements repo, IAnnouncementNotifier notifier) =>
        new(repo, notifier, new SystemClock(), NullLogger<AnnouncementAppService>.Instance);

    internal sealed class MemoryAnnouncements : IAnnouncementRepository
    {
        internal List<Announcement> Items { get; } = [];
        public bool FailSave { get; init; }
        public Task AddAsync(Announcement item, CancellationToken cancellationToken)
        {
            if (FailSave) throw new InvalidOperationException("保存失败");
            Items.Add(item);
            return Task.CompletedTask;
        }
        public Task<Announcement?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(Items.Count);
        public Task<IReadOnlyList<Announcement>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Announcement>>(Items.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToList());
    }

    private sealed class RecordingNotifier(MemoryAnnouncements repository) : IAnnouncementNotifier
    {
        public bool Fail { get; init; }
        public List<AnnouncementDto> Sent { get; } = [];
        public Task BroadcastAsync(AnnouncementDto notification, CancellationToken cancellationToken)
        {
            Assert.Contains(repository.Items, x => x.Id == notification.Id);
            if (Fail) throw new InvalidOperationException("广播失败");
            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }
}
