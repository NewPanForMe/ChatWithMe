using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Application.Users;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Auth;

public sealed class AuthAppService : IAuthAppService
{
    private const string InvalidCredentialsMessage = "邮箱或密码不正确。";
    private const string InvalidRefreshTokenMessage = "刷新令牌无效、已吊销或已过期。";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ITokenHasher _tokenHasher;
    private readonly IClock _clock;
    private readonly ILogger<AuthAppService> _logger;

    public AuthAppService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenIssuer tokenIssuer,
        ITokenHasher tokenHasher,
        IClock clock,
        ILogger<AuthAppService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenIssuer = tokenIssuer;
        _tokenHasher = tokenHasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = TryNormalizeEmail(request.Email);
        var user = email is null ? null : await _userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("登录失败：邮箱 {Email} 不存在或密码不匹配", request.Email);

            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        return await IssueAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthTokenDto> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = await FindActiveAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
        var user = await _userRepository.GetByIdAsync(current.UserId, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new RefreshTokenRejectedException(InvalidRefreshTokenMessage);
        }

        var issued = _tokenIssuer.Issue(user.Id, user.Email.Value, user.Role, user.SessionVersion, AuthClaimNames.PrincipalUser);
        var issuedHash = _tokenHasher.Hash(issued.RefreshToken);

        current.Revoke(_clock.UtcNow, issuedHash);

        await _refreshTokenRepository
            .AddAsync(RefreshToken.Issue(user.Id, issuedHash, issued.RefreshTokenExpiresAtUtc), cancellationToken)
            .ConfigureAwait(false);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("刷新令牌轮换完成：用户 {UserId}，旧令牌已吊销", user.Id);

        return ToDto(user, issued);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tokenHash = HashOrNull(request.RefreshToken);

        if (tokenHash is null)
        {
            return;
        }

        var refreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        if (refreshToken is null || refreshToken.RevokedAtUtc is not null)
        {
            return;
        }

        refreshToken.Revoke(_clock.UtcNow, null);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("登出完成：用户 {UserId} 的刷新令牌已吊销", refreshToken.UserId);
    }

    private async Task<AuthTokenDto> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var issued = _tokenIssuer.Issue(user.Id, user.Email.Value, user.Role, user.SessionVersion, AuthClaimNames.PrincipalUser);

        await _refreshTokenRepository
            .AddAsync(
                RefreshToken.Issue(user.Id, _tokenHasher.Hash(issued.RefreshToken), issued.RefreshTokenExpiresAtUtc),
                cancellationToken)
            .ConfigureAwait(false);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToDto(user, issued);
    }

    private async Task<RefreshToken> FindActiveAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashOrNull(refreshToken);
        var current = tokenHash is null
            ? null
            : await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        if (current is null || !current.IsActive(_clock.UtcNow))
        {
            throw new RefreshTokenRejectedException(InvalidRefreshTokenMessage);
        }

        return current;
    }

    private string? HashOrNull(string? refreshToken) =>
        string.IsNullOrWhiteSpace(refreshToken) ? null : _tokenHasher.Hash(refreshToken);

    private static string? TryNormalizeEmail(string? value)
    {
        try
        {
            return Email.Create(value).Value;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static AuthTokenDto ToDto(User user, IssuedTokenPair issued) => new(
        issued.AccessToken,
        issued.RefreshToken,
        "Bearer",
        issued.AccessTokenExpiresAtUtc,
        issued.RefreshTokenExpiresAtUtc,
        new UserDto(user.Id, user.Email.Value, user.NickName, user.CreatedAtUtc));
}
