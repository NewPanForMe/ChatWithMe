using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatWithMe.Api.Serialization;

/// <summary>
/// 全局 DateTime 序列化口径：数据库中一律存 UTC，JSON 输出统一转换到配置时区
/// （Time:TimeZoneId，默认 Asia/Shanghai）并格式化为 yyyy/MM/dd HH:mm:ss，
/// 不带小数秒、不带 Z/偏移后缀。反序列化把无时区的时间文本按该时区解释后转回 UTC。
/// </summary>
public sealed class LocalDateTimeConverter : JsonConverter<DateTime>
{
    public const string Format = "yyyy/MM/dd HH:mm:ss";

    /// <summary>
    /// 翻页游标专用格式：与展示格式同为配置时区文本，但保留毫秒，保证游标往返不丢精度。
    /// </summary>
    public const string CursorFormat = "yyyy/MM/dd HH:mm:ss.fff";

    private readonly TimeZoneInfo _timeZone;

    public LocalDateTimeConverter(TimeZoneInfo timeZone) => _timeZone = timeZone;

    public static string FormatCursor(DateTime utc, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);

        return local.ToString(CursorFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 把外部时间文本解析成 UTC。既接受接口输出的 yyyy/MM/dd HH:mm:ss（按配置时区解释），
    /// 也接受翻页游标的毫秒格式，以及带偏移的 ISO-8601（按其自身偏移解释）。
    /// </summary>
    public static bool TryParse(string? text, TimeZoneInfo timeZone, out DateTime utc)
    {
        utc = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (DateTime.TryParseExact(text, CursorFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var cursor))
        {
            utc = ToUtc(cursor, timeZone);
            return true;
        }

        if (DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var wallClock))
        {
            utc = ToUtc(wallClock, timeZone);
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            utc = parsed.Kind == DateTimeKind.Utc ? parsed : ToUtc(parsed, timeZone);
            return true;
        }

        return false;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        if (!TryParse(text, _timeZone, out var utc))
        {
            throw new JsonException("无法解析时间值 \"" + text + "\"，期望格式 " + Format + "。");
        }

        return utc;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, _timeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(value, _timeZone),
            _ => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), _timeZone),
        };

        writer.WriteStringValue(local.ToString(Format, CultureInfo.InvariantCulture));
    }

    private static DateTime ToUtc(DateTime value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), timeZone);
}
