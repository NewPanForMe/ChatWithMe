namespace ChatWithMe.Application.Realtime;

/// <summary>强制关闭某用户的所有实时连接（踢人时使用）。</summary>
public interface IOnlineSessionTerminator
{
    Task<int> TerminateAsync(Guid userId, CancellationToken cancellationToken);
}
