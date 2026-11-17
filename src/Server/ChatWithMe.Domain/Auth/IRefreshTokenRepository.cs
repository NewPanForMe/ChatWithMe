namespace ChatWithMe.Domain.Auth;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>吊销某用户全部仍有效的刷新令牌，返回吊销数量（不自动保存）。</summary>
    Task<int> RevokeAllForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
