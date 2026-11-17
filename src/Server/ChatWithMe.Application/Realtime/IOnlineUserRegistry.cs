namespace ChatWithMe.Application.Realtime;

/// <summary>在线连接登记表：记录每个用户的 SignalR 连接数（多端 = 多条连接）。</summary>
public interface IOnlineUserRegistry
{
    void Connect(Guid userId, string connectionId);

    void Disconnect(Guid userId, string connectionId);

    bool IsOnline(Guid userId);

    int GetConnectionCount(Guid userId);

    IReadOnlyCollection<Guid> GetOnlineUserIds();
}
