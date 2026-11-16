namespace ChatWithMe.Domain.Users;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// 按创建时间倒序分页查询用户；keyword 非空时对邮箱与昵称做大小写不敏感的子串匹配。
    /// </summary>
    Task<IReadOnlyList<User>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken);

    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
