using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ChatWithMe.Infrastructure.Messaging;

internal sealed class MongoIndexInitializer : IHostedService
{
    private readonly Lazy<IMongoClient> _mongoClient;
    private readonly MongoOptions _options;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(Lazy<IMongoClient> mongoClient, IOptions<MongoOptions> options, ILogger<MongoIndexInitializer> logger)
    {
        _mongoClient = mongoClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.ServerSelectionTimeoutSeconds > 0 ? _options.ServerSelectionTimeoutSeconds : 5));

            var keys = Builders<MessageDocument>.IndexKeys
                .Ascending(document => document.ConversationId)
                .Descending(document => document.SentAtUtc);

            var collection = _mongoClient.Value
                .GetDatabase(_options.DatabaseName)
                .GetCollection<MessageDocument>(MongoOptions.MessagesCollectionName);

            await collection.Indexes
                .CreateOneAsync(new CreateIndexModel<MessageDocument>(keys, new CreateIndexOptions { Name = "ix_messages_conversation_sent_at" }), cancellationToken: timeoutSource.Token)
                .ConfigureAwait(false);

            _logger.LogInformation("MongoDB 消息集合索引已就绪：{IndexName}", "ix_messages_conversation_sent_at");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "MongoDB 索引创建失败，消息相关端点暂不可用（其他端点仍可访问）：{Reason}", exception.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
