namespace ChatWithMe.Infrastructure.ConnectionStrings;

public interface IConnectionStringProvider
{
    string GetConnectionString(string? connName = null);

    Task<string> GetConnectionStringAsync(string? connName = null, CancellationToken cancellationToken = default);

    /// <summary>最近一次解析状态（远端 / 本地缓存），用于健康检查与启动日志。</summary>
    ConnectionStringStatus GetStatus(string? connName = null);
}
