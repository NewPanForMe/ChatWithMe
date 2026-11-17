using ChatWithMe.Domain.Auth;
using ChatWithMe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Auth;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ChatWithMeDbContext _context;

    public RefreshTokenRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.RefreshTokens.FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

    public async Task<int> RevokeAllForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var active = await _context.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == userId
                && refreshToken.RevokedAtUtc == null
                && refreshToken.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var refreshToken in active)
        {
            refreshToken.Revoke(utcNow, null);
        }

        return active.Count;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
