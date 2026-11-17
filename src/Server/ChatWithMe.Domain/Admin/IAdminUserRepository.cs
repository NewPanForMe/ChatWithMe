namespace ChatWithMe.Domain.Admin;

public interface IAdminUserRepository
{
    Task AddAsync(AdminUser user, CancellationToken cancellationToken);

    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
