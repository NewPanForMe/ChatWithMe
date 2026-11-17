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

    public async Task<Message> AddAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var collection = GetCollection();

        try
        {
            await collection.InsertOneAsync(MessageDocument.FromDomain(message), cancellationToken: cancellationToken).ConfigureAwait(false);

            return message;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey && message.ClientMessageId is not null)
        {
            // 幂等命中：同一会话同一 clientMessageId 只落一条，直接返回已存在的消息。
            var existing = await GetByClientMessageIdAsync(message.ConversationId, message.ClientMessageId, cancellationToken).ConfigureAwait(false);

            return existing ?? throw new MongoUnavailableException(
                "写入消息触发唯一键冲突，但未能读回已存在的消息：" + exception.Message,
                exception);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new MongoUnavailableException("写入 MongoDB 消息集合失败：" + exception.Message, exception);
        }
    }

    public async Task<Message?> GetByClientMessageIdAsync(Guid conversationId, string clientMessageId, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<MessageDocument>.Filter.And(
                Builders<MessageDocument>.Filter.Eq(document => document.ConversationId, conversationId.ToString()),
                Builders<MessageDocument>.Filter.Eq(document => document.ClientMessageId, clientMessageId));

            var document = await GetCollection()
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return document?.ToDomain();
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new MongoUnavailableException("读取 MongoDB 消息集合失败：" + exception.Message, exception);
        }
    }

    public async Task<IReadOnlyList<Message>> GetByConversationAsync(Guid conversationId, MessageCursor? before, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<MessageDocument>.Filter.Eq(document => document.ConversationId, conversationId.ToString());

            if (before is not null)
            {
                filter &= BuildCursorFilter(before);
            }

            var sort = Builders<MessageDocument>.Sort
                .Descending(document => document.SentAtUtc)
                .Descending(document => document.Id);

            var documents = await GetCollection()
                .Find(filter)
                .Sort(sort)
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

    /// <summary>
    /// 复合游标过滤：<c>SentAtUtc &lt; t</c>，或 <c>SentAtUtc == t 且 Id &lt; id</c>。
    /// 只带时间戳的旧式游标退化为 <c>SentAtUtc &lt; t</c>（与改动前行为一致）。
    /// </summary>
    private static FilterDefinition<MessageDocument> BuildCursorFilter(MessageCursor before)
    {
        var sentAtUtc = before.SentAtUtc.Kind == DateTimeKind.Utc
            ? before.SentAtUtc
            : before.SentAtUtc.ToUniversalTime();

        if (before.MessageId is null)
        {
            return Builders<MessageDocument>.Filter.Lt(document => document.SentAtUtc, sentAtUtc);
        }

        var messageId = before.MessageId.Value.ToString();

        return Builders<MessageDocument>.Filter.Or(
            Builders<MessageDocument>.Filter.Lt(document => document.SentAtUtc, sentAtUtc),
            Builders<MessageDocument>.Filter.And(
                Builders<MessageDocument>.Filter.Eq(document => document.SentAtUtc, sentAtUtc),
                Builders<MessageDocument>.Filter.Lt(document => document.Id, messageId)));
    }

    private IMongoCollection<MessageDocument> GetCollection() =>
        _mongoClient.Value
            .GetDatabase(_options.DatabaseName)
            .GetCollection<MessageDocument>(MongoOptions.MessagesCollectionName);
}
