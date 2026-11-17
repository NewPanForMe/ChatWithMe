using System.Collections.Concurrent;
using ChatWithMe.Application.Realtime;

namespace ChatWithMe.Api.Realtime;

/// <summary>进程内在线登记表（单实例部署；后续上多实例时替换为 Redis 实现）。</summary>
public sealed class InMemoryOnlineUserRegistry : IOnlineUserRegistry
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();

    public void Connect(Guid userId, string connectionId)
    {
        var connections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        connections[connectionId] = 0;
    }

    public void Disconnect(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var connections))
        {
            return;
        }

        connections.TryRemove(connectionId, out _);

        if (connections.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }
    }

    public bool IsOnline(Guid userId) => _connections.ContainsKey(userId);

    public int GetConnectionCount(Guid userId) =>
        _connections.TryGetValue(userId, out var connections) ? connections.Count : 0;

    public IReadOnlyCollection<Guid> GetOnlineUserIds() => _connections.Keys.ToList();
}
