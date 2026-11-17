using ChatWithMe.Application.Users;
using ChatWithMe.Domain.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace ChatWithMe.Api.Hosting;

/// <summary>
/// 管理后台账号引导：库里没有任何管理员时，按配置创建默认管理员。
/// 管理端账号与聊天用户分离，不再把聊天用户提升为管理员。
/// 配置项：Security:BootstrapAdminEmail / Security:BootstrapAdminPassword（仅开发环境使用，生产必须改）。
/// </summary>
internal sealed class AdminBootstrapService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdminBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["Security:BootstrapAdminEmail"];
        var password = configuration["Security:BootstrapAdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var adminUsers = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();

            if (await adminUsers.AnyAsync(cancellationToken))
            {
                return;
            }

            var admin = AdminUser.Create(email, "默认管理员", PasswordHasher.Hash(password));

            await adminUsers.AddAsync(admin, cancellationToken);
            await adminUsers.SaveChangesAsync(cancellationToken);

            logger.LogWarning("已创建默认管理员 {Email}（配置项 Security:BootstrapAdminEmail/Password，生产环境请立即修改）", email);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "创建默认管理员失败（不影响启动）：{Reason}", exception.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
