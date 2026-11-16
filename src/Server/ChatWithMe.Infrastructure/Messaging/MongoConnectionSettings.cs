using ChatWithMe.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ChatWithMe.Infrastructure.Messaging;

public static class MongoConnectionSettings
{
    public const string DefaultConnectionName = "ChatWithMeMongo";

    public const string ConnectionNameConfigurationKey = "Security:ConnectionNames:Mongo";

    public static string ResolveConnectionName(IConfiguration configuration)
    {
        var connName = configuration[ConnectionNameConfigurationKey];

        return string.IsNullOrWhiteSpace(connName) ? DefaultConnectionName : connName.Trim();
    }

    public static IMongoClient CreateClient(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var mongoOptions = serviceProvider.GetRequiredService<IOptions<MongoOptions>>().Value;
        var connectionStringProvider = serviceProvider.GetRequiredService<ConnectionStrings.IConnectionStringProvider>();
        var connName = ResolveConnectionName(configuration);

        string connectionString;

        try
        {
            connectionString = connectionStringProvider.GetConnectionString(connName);
        }
        catch (Exception exception)
        {
            throw new MongoUnavailableException(
                "无法取得 MongoDB 连接串：连接名 " + connName + "（配置项 " + ConnectionNameConfigurationKey +
                " 的当前值）。请确认远端 ToolsConsole 上已存在该连接名，且 Security:DeliveryKey 正确。原因：" + exception.Message,
                exception);
        }

        var settings = BuildClientSettings(connectionString, mongoOptions);

        return new MongoClient(settings);
    }

    /// <summary>
    /// 由远端连接串构造客户端设置：远端下发的连接串不含 authSource（形如
    /// mongodb://user:pwd@host:27017/ChatWithMe），而账号建在 admin 库，驱动会默认拿
    /// ChatWithMe 库去认证，导致 SCRAM-SHA-256 认证失败。此处用 Mongo:AuthSource 补齐。
    /// </summary>
    public static MongoClientSettings BuildClientSettings(string connectionString, MongoOptions mongoOptions)
    {
        var urlBuilder = new MongoUrlBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(mongoOptions.AuthSource) && string.IsNullOrWhiteSpace(urlBuilder.AuthenticationSource))
        {
            urlBuilder.AuthenticationSource = mongoOptions.AuthSource.Trim();
        }

        var timeout = TimeSpan.FromSeconds(mongoOptions.ServerSelectionTimeoutSeconds > 0 ? mongoOptions.ServerSelectionTimeoutSeconds : 5);
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        settings.ServerSelectionTimeout = timeout;
        settings.ConnectTimeout = timeout;

        return settings;
    }
}
