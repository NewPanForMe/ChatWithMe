namespace ChatWithMe.Domain.Messages;

/// <summary>
/// 消息倒序翻页游标：<c>(SentAtUtc, Id)</c> 复合键。
/// 只按时间戳翻页时，同一毫秒内的多条消息在跨页处会重复或遗漏；加上 Id 作为次序兜底后，
/// 排序变成严格全序，翻页结果稳定。Id 为 <c>null</c> 表示只有时间戳的旧式游标（兼容历史客户端）。
/// </summary>
public sealed record MessageCursor(DateTime SentAtUtc, Guid? MessageId = null)
{
    /// <summary>用一条已存储的消息构造"从这条之后继续"的游标。</summary>
    public static MessageCursor After(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MessageCursor(message.SentAtUtc, message.Id);
    }
}
