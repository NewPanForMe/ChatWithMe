using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Admin;

public sealed class AdminUserAppService : IAdminUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOnlineUserRegistry _onlineUserRegistry;
    private readonly IOnlineSessionTerminator _sessionTerminator;
    private readonly IClock _clock;
    private readonly ILogger<AdminUserAppService> _logger;

    public AdminUserAppService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOnlineUserRegistry onlineUserRegistry,
        IOnlineSessionTerminator sessionTerminator,
        IClock clock,
        ILogger<AdminUserAppService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _onlineUserRegistry = onlineUserRegistry;
        _sessionTerminator = sessionTerminator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OnlineUserDto>> ListOnlineAsync(CancellationToken cancellationToken)
    {
        var onlineUserIds = _onlineUserRegistry.GetOnlineUserIds();

        if (onlineUserIds.Count == 0)
        {
            return [];
        }

        var users = await _userRepository.GetByIdsAsync(onlineUserIds, cancellationToken).ConfigureAwait(false);

        return users
            .Select(user => new OnlineUserDto(
                user.Id,
                user.Email.Value,
                user.NickName,
                _onlineUserRegistry.GetConnectionCount(user.Id)))
            .OrderBy(user => user.NickName, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<KickResultDto> KickAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("用户不存在。");

        user.InvalidateSessions();
        await _userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var revoked = await _refreshTokenRepository
            .RevokeAllForUserAsync(userId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var closed = await _sessionTerminator.TerminateAsync(userId, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "管理员强制下线用户 {UserId}：会话版本 {SessionVersion}，吊销刷新令牌 {Revoked} 个，断开连接 {Closed} 个",
            userId,
            user.SessionVersion,
            revoked,
            closed);

        return new KickResultDto(userId, user.SessionVersion, revoked, closed);
    }
}
