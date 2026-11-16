using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Messages;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ChatWithMe.Infrastructure.Messaging;

public sealed class MongoMessageRepository : IMessageRepository
{
    private readonly Lazy<IMongoClient> _mongoClient;
    private readonly MongoOptions _options;

    public MongoMessageRepository(Lazy<IMongoClient> mongoClient, IOptions<MongoOptions> options)
    {
        _mongoClient = mongoClient;
        _options = options.Value;
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            await GetCollection().InsertOneAsync(MessageDocument.FromDomain(message), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new MongoUnavailableException("写入 MongoDB 消息集合失败：" + exception.Message, exception);
        }
    }

    public async Task<IReadOnlyList<Message>> GetByConversationAsync(Guid conversationId, DateTime? beforeUtc, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<MessageDocument>.Filter.Eq(document => document.ConversationId, conversationId.ToString());

            if (beforeUtc.HasValue)
            {
                filter &= Builders<MessageDocument>.Filter.Lt(document => document.SentAtUtc, beforeUtc.Value);
            }

            var documents = await GetCollection()
                .Find(filter)
                .Sort(Builders<MessageDocument>.Sort.Descending(document => document.SentAtUtc))
                .Limit(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return documents.Select(document => document.ToDomain()).ToList();
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new MongoUnavailableException("读取 MongoDB 消息集合失败：" + exception.Message, exception);
        }
    }

    private IMongoCollection<MessageDocument> GetCollection() =>
        _mongoClient.Value
            .GetDatabase(_options.DatabaseName)
            .GetCollection<MessageDocument>(MongoOptions.MessagesCollectionName);
}
