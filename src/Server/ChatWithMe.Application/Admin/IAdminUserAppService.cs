namespace ChatWithMe.Application.Admin;

public sealed record OnlineUserDto(Guid Id, string Email, string NickName, int Connections);

public sealed record KickResultDto(Guid UserId, int SessionVersion, int RevokedRefreshTokens, int ClosedConnections);

public interface IAdminUserAppService
{
    Task<IReadOnlyList<OnlineUserDto>> ListOnlineAsync(CancellationToken cancellationToken);

    /// <summary>强制下线：自增会话版本、吊销全部刷新令牌、断开实时连接。</summary>
    Task<KickResultDto> KickAsync(Guid userId, CancellationToken cancellationToken);
}
