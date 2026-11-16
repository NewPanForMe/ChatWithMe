namespace ChatWithMe.Domain.Auth;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
