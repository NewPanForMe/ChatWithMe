using ChatWithMe.Domain.Messages;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatWithMe.Infrastructure.Messaging;

internal sealed class MessageDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string ConversationId { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SentAtUtc { get; set; }

    public static MessageDocument FromDomain(Message message) => new()
    {
        Id = message.Id.ToString(),
        ConversationId = message.ConversationId.ToString(),
        SenderId = message.SenderId.ToString(),
        Content = message.Content,
        SentAtUtc = message.SentAtUtc,
    };

    public Message ToDomain() => Message.Restore(
        Guid.Parse(Id),
        Guid.Parse(ConversationId),
        Guid.Parse(SenderId),
        Content,
        DateTime.SpecifyKind(SentAtUtc, DateTimeKind.Utc));
}
