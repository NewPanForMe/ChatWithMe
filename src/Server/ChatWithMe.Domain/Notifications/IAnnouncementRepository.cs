namespace ChatWithMe.Domain.Notifications;

public interface IAnnouncementRepository
{
    Task AddAsync(Announcement item, CancellationToken cancellationToken);
    Task<Announcement?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Announcement>> ListAsync(int skip, int take, CancellationToken cancellationToken);
}
