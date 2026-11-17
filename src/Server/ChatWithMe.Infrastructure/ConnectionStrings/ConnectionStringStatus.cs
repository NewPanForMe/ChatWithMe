namespace ChatWithMe.Infrastructure.ConnectionStrings;

/// <summary>连接串来源：远端下发 / 本地加密缓存兜底 / 尚未解析成功。</summary>
public enum ConnectionStringSource
{
    Unknown,
    Remote,
    Cache,
}

/// <summary>
/// 最近一次连接串解析结果，用于启动日志与 <c>/api/health/db</c> 显式暴露外部依赖状态。
/// </summary>
public sealed record ConnectionStringStatus(
    string ConnectionName,
    ConnectionStringSource Source,
    DateTime? ResolvedAtUtc,
    DateTime? CachedAtUtc,
    string? Endpoint,
    string? Detail)
{
    public static ConnectionStringStatus Unknown(string connectionName) =>
        new(connectionName, ConnectionStringSource.Unknown, null, null, null, "尚未解析（或解析尚未完成）");
}
