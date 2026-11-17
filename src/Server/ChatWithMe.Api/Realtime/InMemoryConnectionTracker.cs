using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ChatWithMe.Api.Realtime;

/// <summary>保存每个连接的 HubCallerContext，用于管理员踢人时服务端强制断开。</summary>
public interface IConnectionTracker
{
    void Add(Guid userId, string connectionId, HubCallerContext context);

    void Remove(string connectionId);

    IReadOnlyList<HubCallerContext> GetContexts(Guid userId);
}

public sealed class InMemoryConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, (Guid UserId, HubCallerContext Context)> _connections = new();

    public void Add(Guid userId, string connectionId, HubCallerContext context) =>
        _connections[connectionId] = (userId, context);

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

    public IReadOnlyList<HubCallerContext> GetContexts(Guid userId) =>
        _connections.Values
            .Where(entry => entry.UserId == userId)
            .Select(entry => entry.Context)
            .ToList();
}
