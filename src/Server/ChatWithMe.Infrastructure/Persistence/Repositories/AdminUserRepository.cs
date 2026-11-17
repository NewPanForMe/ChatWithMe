using ChatWithMe.Domain.Admin;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly ChatWithMeDbContext _context;

    public AdminUserRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(AdminUser user, CancellationToken cancellationToken)
    {
        _context.AdminUsers.Add(user);
        return Task.CompletedTask;
    }

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AdminUsers.FirstOrDefaultAsync(admin => admin.Id == id, cancellationToken);

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.AdminUsers.FirstOrDefaultAsync(admin => admin.Email == email, cancellationToken);

    public async Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken) =>
        await _context.AdminUsers
            .AsNoTracking()
            .OrderBy(admin => admin.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        _context.AdminUsers.AnyAsync(cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AdminUsers.Where(admin => admin.Id == id).ExecuteDeleteAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
