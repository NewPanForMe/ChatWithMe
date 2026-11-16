using ChatWithMe.Application.Common;
using ChatWithMe.Application.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatWithMe.Infrastructure.Messaging;

public sealed class MongoHealthProbe : IMongoHealthProbe
{
    private readonly Lazy<IMongoClient> _mongoClient;
    private readonly MongoOptions _options;
    private readonly string _connectionName;

    public MongoHealthProbe(Lazy<IMongoClient> mongoClient, IOptions<MongoOptions> options, IConfiguration configuration)
    {
        _mongoClient = mongoClient;
        _options = options.Value;
        _connectionName = MongoConnectionSettings.ResolveConnectionName(configuration);
    }

    public async Task<MongoHealthResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _mongoClient.Value
                .GetDatabase(_options.DatabaseName)
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new MongoHealthResult(
                true,
                "MongoDB ping 成功，库 " + _options.DatabaseName + "，集合 " + MongoOptions.MessagesCollectionName);
        }
        catch (MongoUnavailableException exception)
        {
            return new MongoHealthResult(false, exception.Message);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            return new MongoHealthResult(
                false,
                "无法连接 MongoDB：连接名 " + _connectionName + "（配置项 " + MongoConnectionSettings.ConnectionNameConfigurationKey +
                "）解析出的连接串对应实例不可达。原因：" + exception.Message);
        }
    }
}
