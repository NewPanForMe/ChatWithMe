using ChatWithMe.Application.Messages;
using ChatWithMe.Infrastructure.ConnectionStrings;
using ChatWithMe.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(
    ChatWithMeDbContext dbContext,
    IMongoHealthProbe mongoHealthProbe,
    IConnectionStringProvider connectionStringProvider,
    ILogger<HealthController> logger) : ControllerBase
{
    /// <summary>
    /// 数据库连通性。<c>connectionStringSource</c> 显式暴露连接串来自远端还是本地缓存，
    /// 避免"外部依赖降级"静默不可见。
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> GetDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var status = connectionStringProvider.GetStatus();

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return Ok(new DatabaseHealthResponse(
                canConnect ? "ok" : "unreachable",
                status.Source.ToString().ToLowerInvariant(),
                status.CachedAtUtc,
                canConnect ? "连接串解密成功，远端 PostgreSQL 连接可用" : "CanConnectAsync 返回 false"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据库连通性检查失败");

            return Ok(new DatabaseHealthResponse("unreachable", status.Source.ToString().ToLowerInvariant(), status.CachedAtUtc, exception.Message));
        }
    }

    [HttpGet("mongo")]
    public async Task<IActionResult> GetMongoHealthAsync(CancellationToken cancellationToken)
    {
        var result = await mongoHealthProbe.CheckAsync(cancellationToken);

        if (result.Ok)
        {
            return Ok(new MongoHealthResponse("ok", result.Detail));
        }

        logger.LogError("MongoDB 连通性检查失败：{Detail}", result.Detail);

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "MongoDB 不可用",
            Detail = result.Detail,
        });
    }

    public sealed record DatabaseHealthResponse(string Database, string ConnectionStringSource, DateTime? ConnectionStringCachedAtUtc, string Detail);

    public sealed record MongoHealthResponse(string Mongo, string Detail);
}
