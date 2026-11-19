using ChatWithMe.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class AnnouncementRepository(ChatWithMeDbContext context) : IAnnouncementRepository
{
    public async Task AddAsync(Announcement item, CancellationToken cancellationToken)
    {
        await context.Set<Announcement>().AddAsync(item, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Announcement?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<Announcement>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) => context.Set<Announcement>().CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Announcement>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
        await context.Set<Announcement>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
}
