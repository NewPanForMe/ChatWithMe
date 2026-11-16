using ChatWithMe.Domain.Users;
using ChatWithMe.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ChatWithMeDbContext _context;

    public UserRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(user => user.Email.Value == email, cancellationToken);

    public Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(user => user.Email.Value == email, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken) =>
        await BuildQuery(keyword)
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenByDescending(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountAsync(string? keyword, CancellationToken cancellationToken) =>
        BuildQuery(keyword).CountAsync(cancellationToken);

    private IQueryable<User> BuildQuery(string? keyword)
    {
        var query = _context.Users.AsNoTracking();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var pattern = LikePatterns.Contains(keyword);

        return query.Where(user =>
            EF.Functions.ILike(user.Email.Value, pattern, LikePatterns.EscapeCharacter) ||
            EF.Functions.ILike(user.NickName, pattern, LikePatterns.EscapeCharacter));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
