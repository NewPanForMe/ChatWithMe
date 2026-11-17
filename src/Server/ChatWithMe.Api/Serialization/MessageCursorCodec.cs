using System.Globalization;
using ChatWithMe.Domain.Messages;

namespace ChatWithMe.Api.Serialization;

/// <summary>
/// 消息翻页游标的文本编解码：<c>yyyy/MM/dd HH:mm:ss.fff|{messageId:N}</c>（配置时区口径）。
/// 时间戳部分保留毫秒，Id 部分保证同一毫秒内的多条消息也能稳定翻页；
/// 不带 <c>|</c> 的历史游标按"只有时间戳"解析，保持向后兼容。
/// </summary>
public static class MessageCursorCodec
{
    public const char Separator = '|';

    public static string Encode(MessageCursor cursor, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        var timestamp = LocalDateTimeConverter.FormatCursor(cursor.SentAtUtc, timeZone);

        return cursor.MessageId is null
            ? timestamp
            : timestamp + Separator.ToString(CultureInfo.InvariantCulture) + cursor.MessageId.Value.ToString("N");
    }

    public static bool TryDecode(string? text, TimeZoneInfo timeZone, out MessageCursor? cursor)
    {
        cursor = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        var separatorIndex = value.LastIndexOf(Separator);

        if (separatorIndex < 0)
        {
            if (!LocalDateTimeConverter.TryParse(value, timeZone, out var legacyTimestamp))
            {
                return false;
            }

            cursor = new MessageCursor(legacyTimestamp);

            return true;
        }

        var timestampText = value[..separatorIndex];
        var messageIdText = value[(separatorIndex + 1)..];

        if (!LocalDateTimeConverter.TryParse(timestampText, timeZone, out var sentAtUtc) ||
            !Guid.TryParse(messageIdText, out var messageId))
        {
            return false;
        }

        cursor = new MessageCursor(sentAtUtc, messageId);

        return true;
    }
}
