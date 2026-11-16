using ChatWithMe.Infrastructure.ConnectionStrings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatWithMe.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChatWithMeDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__ChatWithMe";
    private const string DeliveryKeyEnvironmentVariable = "Security__DeliveryKey";

    public ChatWithMeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = ResolveFromDeliveryKey();
        }

        var options = new DbContextOptionsBuilder<ChatWithMeDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ChatWithMeDbContext(options);
    }

    private static string ResolveFromDeliveryKey()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DeliveryKeyEnvironmentVariable)))
        {
            throw new InvalidOperationException(
                "dotnet ef 需要连接串：请设置环境变量 " + ConnectionStringEnvironmentVariable +
                "（完整连接串），或设置环境变量 " + DeliveryKeyEnvironmentVariable +
                "（Security:DeliveryKey，32 字节密钥的 Base64）后重试。");
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IConnectionStringProvider, RuntimeConnectionStringProvider>();

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IConnectionStringProvider>().GetConnectionString();
    }
}
